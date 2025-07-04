using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NBitcoin;

// คลาสสำหรับเก็บข้อมูล Config ที่จะเซฟเป็นไฟล์ JSON
public class AppConfig
{
    public Dictionary<string, string> SavedXpubs { get; set; } = new Dictionary<string, string>();
}

// *** ใหม่: เพิ่ม Context สำหรับ JSON Source Generation ***
// คลาสนี้จะช่วยให้ JsonSerializer สร้างโค้ดสำหรับ Serialize/Deserialize ตอนคอมไพล์
// เพื่อแก้ปัญหา "Reflection-based serialization has been disabled"
[JsonSerializable(typeof(AppConfig))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

public class BtcUtilityApp
{
    private const int GapLimit = 20;
    private const string ConfigFileName = "app_config.json";
    private static AppConfig _config = new AppConfig();

    // *** ใหม่: สร้าง JsonSerializerOptions ที่ใช้ Source Generator ***
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        TypeInfoResolver = AppJsonSerializerContext.Default
    };

    public static async Task Main(string[] args)
    {
        Console.Title = "BTC Utility Tool v3 (with Config)";
        LoadConfig(); // โหลด Config ตอนเริ่มโปรแกรม

        while (true)
        {
            ShowMainMenu();
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    HandleGenerateAddresses();
                    break;
                case "2":
                    await HandleCheckBalance();
                    break;
                case "3":
                    HandleManageConfig();
                    break;
                case "0":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("กำลังออกจากโปรแกรม...");
                    Console.ResetColor();
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ตัวเลือกไม่ถูกต้อง กรุณาลองใหม่อีกครั้ง");
                    Console.ResetColor();
                    break;
            }

            if (choice != "3") // ไม่ต้องรอถ้าเพิ่งออกจากเมนูจัดการ
            {
                Console.WriteLine("\nกดปุ่มใดก็ได้เพื่อกลับสู่เมนูหลัก...");
                Console.ReadKey();
            }
        }
    }

    private static void ShowMainMenu()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=====================================");
        Console.WriteLine("   BTC Utility Tool by NBitcoin v3   ");
        Console.WriteLine("=====================================");
        Console.ResetColor();
        Console.WriteLine("กรุณาเลือกเมนูที่ต้องการ:");
        Console.WriteLine("  1. Generate Public Addresses");
        Console.WriteLine("  2. Check Wallet Balance from xpub");
        Console.WriteLine("  3. Manage Saved xpubs");
        Console.WriteLine("-------------------------------------");
        Console.WriteLine("  0. Exit");
        Console.Write("\nเลือก (0-3): ");
    }

    #region Handlers
    private static void HandleGenerateAddresses()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("--- Generate Public Addresses ---");
        Console.ResetColor();

        try
        {
            string? xpub = GetXpubFromUser();
            if (string.IsNullOrWhiteSpace(xpub)) return;

            Network network = GetNetworkFromUser();
            ScriptPubKeyType addressType = GetAddressTypeFromUser();

            Console.WriteLine("\nกำลังสร้าง 20 Addresses แรก (Index 0-19)...");
            Console.WriteLine("------------------------------------------");
            for (int i = 0; i < 20; i++)
            {
                BitcoinAddress address = GenerateAddressByIndex(xpub, (uint)i, network, addressType);
                Console.WriteLine($"  Index {i,-2}: {address}");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private static async Task HandleCheckBalance()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("--- Check Wallet Balance ---");
        Console.ResetColor();

        try
        {
            string? xpub = GetXpubFromUser();
            if (string.IsNullOrWhiteSpace(xpub)) return;

            Network network = GetNetworkFromUser();
            ScriptPubKeyType addressType = GetAddressTypeFromUser();
            string apiEndpoint = network == Network.Main ? "api" : "api/testnet";

            Console.WriteLine("\nกำลังสแกนยอดเงินจาก blockstream.info (อาจใช้เวลาสักครู่)...");
            Console.WriteLine($"Gap Limit = {GapLimit} (จะหยุดเมื่อเจอ Address ว่างติดต่อกัน {GapLimit} อัน)");
            Console.WriteLine("------------------------------------------------------------------");

            long totalBalanceSatoshis = 0;
            int consecutiveUnused = 0;
            uint index = 0;

            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://blockstream.info/");

            while (consecutiveUnused < GapLimit)
            {
                BitcoinAddress address = GenerateAddressByIndex(xpub, index, network, addressType);
                Console.Write($"  Scanning Index {index,-4} ({address.ToString().Substring(0, 10)}...): ");

                try
                {
                    var response = await client.GetStringAsync($"{apiEndpoint}/address/{address}");
                    var addressInfo = JsonDocument.Parse(response);
                    int txCount = addressInfo.RootElement.GetProperty("chain_stats").GetProperty("tx_count").GetInt32();

                    if (txCount > 0)
                    {
                        long fundedSum = addressInfo.RootElement.GetProperty("chain_stats").GetProperty("funded_txo_sum").GetInt64();
                        long spentSum = addressInfo.RootElement.GetProperty("chain_stats").GetProperty("spent_txo_sum").GetInt64();
                        long balance = fundedSum - spentSum;
                        totalBalanceSatoshis += balance;
                        consecutiveUnused = 0;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Found! Balance: {Money.Satoshis(balance).ToDecimal(MoneyUnit.BTC)} BTC");
                        Console.ResetColor();
                    }
                    else
                    {
                        consecutiveUnused++;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Empty");
                        Console.ResetColor();
                    }
                }
                catch (HttpRequestException)
                {
                    consecutiveUnused++;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Empty");
                    Console.ResetColor();
                }
                index++;
            }

            Console.WriteLine("------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"การสแกนเสร็จสิ้น!");
            Console.WriteLine($"ยอดเงินรวมทั้งหมด: {Money.Satoshis(totalBalanceSatoshis).ToDecimal(MoneyUnit.BTC)} BTC");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }
    #endregion

    #region Config Management
    private static void HandleManageConfig()
    {
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("--- Manage Saved xpubs ---");
            Console.ResetColor();
            Console.WriteLine("  1. View Saved xpubs");
            Console.WriteLine("  2. Add New xpub");
            Console.WriteLine("  3. Delete an xpub");
            Console.WriteLine("--------------------------");
            Console.WriteLine("  0. Back to Main Menu");
            Console.Write("\nเลือก (0-3): ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ViewSavedXpubs();
                    break;
                case "2":
                    AddSavedXpub();
                    break;
                case "3":
                    DeleteSavedXpub();
                    break;
                case "0":
                    return;
                default:
                    ShowError("ตัวเลือกไม่ถูกต้อง");
                    break;
            }
            Console.WriteLine("\nกดปุ่มใดก็ได้เพื่อดำเนินการต่อ...");
            Console.ReadKey();
        }
    }

    private static void ViewSavedXpubs()
    {
        Console.WriteLine("\n--- Saved xpubs ---");
        if (_config.SavedXpubs.Count == 0)
        {
            Console.WriteLine("ยังไม่มี xpub ที่บันทึกไว้");
            return;
        }

        foreach (var entry in _config.SavedXpubs)
        {
            Console.WriteLine($"  Alias: {entry.Key}");
            Console.WriteLine($"  xpub:  {entry.Value.Substring(0, 15)}...");
            Console.WriteLine();
        }
    }

    private static void AddSavedXpub()
    {
        Console.WriteLine("\n--- Add New xpub ---");
        Console.Write("ตั้งชื่อเล่น (Alias) สำหรับ xpub นี้: ");
        string? alias = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(alias) || _config.SavedXpubs.ContainsKey(alias))
        {
            ShowError("ชื่อเล่นไม่ถูกต้องหรือมีอยู่แล้ว");
            return;
        }

        Console.Write("วาง xpub/ypub/zpub ของคุณ: ");
        string? xpub = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(xpub))
        {
            ShowError("xpub ไม่สามารถเป็นค่าว่างได้");
            return;
        }

        _config.SavedXpubs[alias] = xpub;
        SaveConfig();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nบันทึก xpub เรียบร้อยแล้ว!");
        Console.ResetColor();
    }

    private static void DeleteSavedXpub()
    {
        Console.WriteLine("\n--- Delete an xpub ---");
        if (_config.SavedXpubs.Count == 0)
        {
            Console.WriteLine("ยังไม่มี xpub ที่บันทึกไว้ให้ลบ");
            return;
        }

        var aliases = _config.SavedXpubs.Keys.ToList();
        for (int i = 0; i < aliases.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {aliases[i]}");
        }
        Console.WriteLine("  0. Cancel");
        Console.Write("\nเลือก xpub ที่ต้องการลบ: ");
        if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= aliases.Count)
        {
            string aliasToDelete = aliases[choice - 1];
            _config.SavedXpubs.Remove(aliasToDelete);
            SaveConfig();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nลบ '{aliasToDelete}' เรียบร้อยแล้ว!");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("ยกเลิกการลบ");
        }
    }

    private static void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFileName))
            {
                string json = File.ReadAllText(ConfigFileName);
                // *** แก้ไข: ใช้ Options ที่มี TypeInfoResolver ***
                _config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            ShowError($"ไม่สามารถโหลดไฟล์ config: {ex.Message}");
            _config = new AppConfig();
        }
    }

    private static void SaveConfig()
    {
        try
        {
            // *** แก้ไข: ใช้ Options ที่มี TypeInfoResolver ***
            string json = JsonSerializer.Serialize(_config, _jsonOptions);
            File.WriteAllText(ConfigFileName, json);
        }
        catch (Exception ex)
        {
            ShowError($"ไม่สามารถบันทึกไฟล์ config: {ex.Message}");
        }
    }
    #endregion

    #region User Input Helpers
    private static string? GetXpubFromUser()
    {
        Console.WriteLine("\nคุณต้องการใช้ xpub แบบใด?");
        Console.WriteLine("  1. ใช้ xpub ที่บันทึกไว้");
        Console.WriteLine("  2. ป้อน xpub ใหม่");
        Console.Write("เลือก (1-2): ");
        string? choice = Console.ReadLine();

        if (choice == "1")
        {
            if (_config.SavedXpubs.Count == 0)
            {
                Console.WriteLine("\nยังไม่มี xpub ที่บันทึกไว้ กรุณาป้อนใหม่");
                return Console.ReadLine();
            }

            var aliases = _config.SavedXpubs.Keys.ToList();
            for (int i = 0; i < aliases.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {aliases[i]}");
            }
            Console.Write("\nเลือก xpub ที่ต้องการใช้: ");
            if (int.TryParse(Console.ReadLine(), out int xpubChoice) && xpubChoice > 0 && xpubChoice <= aliases.Count)
            {
                return _config.SavedXpubs[aliases[xpubChoice - 1]];
            }
            else
            {
                ShowError("ตัวเลือกไม่ถูกต้อง");
                return null;
            }
        }
        else if (choice == "2")
        {
            Console.Write("กรุณาวาง xpub/ypub/zpub ของคุณ: ");
            return Console.ReadLine();
        }
        else
        {
            ShowError("ตัวเลือกไม่ถูกต้อง");
            return null;
        }
    }

    private static Network GetNetworkFromUser()
    {
        while (true)
        {
            Console.WriteLine("\nกรุณาเลือก Network:");
            Console.WriteLine("  1. Main (เครือข่ายจริง)");
            Console.WriteLine("  2. Testnet");
            Console.Write("เลือก (1-2): ");
            string? netChoice = Console.ReadLine();
            if (netChoice == "1") return Network.Main;
            if (netChoice == "2") return Network.TestNet;
            ShowError("ตัวเลือก Network ไม่ถูกต้อง");
        }
    }

    private static ScriptPubKeyType GetAddressTypeFromUser()
    {
        while (true)
        {
            Console.WriteLine("\nกรุณาเลือกประเภทของ Address ที่ต้องการสร้าง:");
            Console.WriteLine("  1. Legacy (ขึ้นต้นด้วย 1)");
            Console.WriteLine("  2. Segwit (P2SH) (ขึ้นต้นด้วย 3)");
            Console.WriteLine("  3. Native Segwit (Bech32) (ขึ้นต้นด้วย bc1)");
            Console.Write("เลือก (1-3): ");
            string? typeChoice = Console.ReadLine();
            switch (typeChoice)
            {
                case "1": return ScriptPubKeyType.Legacy;
                case "2": return ScriptPubKeyType.SegwitP2SH;
                case "3": return ScriptPubKeyType.Segwit;
                default:
                    ShowError("ตัวเลือกประเภท Address ไม่ถูกต้อง");
                    break;
            }
        }
    }

    private static BitcoinAddress GenerateAddressByIndex(string xpubString, uint addressIndex, Network network, ScriptPubKeyType addressType)
    {
        var extPubKey = new BitcoinExtPubKey(xpubString, network);
        KeyPath derivationPath = new KeyPath($"0/{addressIndex}");
        ExtPubKey derivedPubKey = extPubKey.Derive(derivationPath);
        return derivedPubKey.PubKey.GetAddress(addressType, network);
    }

    private static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    #endregion
}
