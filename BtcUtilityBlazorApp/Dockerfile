# ============================================================
# Stage 1: Build Stage - ใช้ .NET SDK เพื่อคอมไพล์แอป
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# คัดลอกไฟล์ .csproj และทำการ Restore ก่อนเพื่อใช้ประโยชน์จาก Docker Caching
COPY *.csproj .
RUN dotnet restore

# คัดลอกไฟล์ทั้งหมดในโปรเจกต์
COPY . .

# *** ใหม่: ทำให้ Dynamic โดยการแทนที่ base href โดยอัตโนมัติ ***
# คำสั่งนี้จะเปลี่ยน <base href="/" /> ไปเป็น <base href="__BASE_PATH__" />
# ทำให้โค้ดต้นฉบับของคุณพร้อมสำหรับ Local Debug ได้เสมอ
RUN sed -i 's|<base href="/" />|<base href="__BASE_PATH__" />|' /src/wwwroot/index.html

# ทำการ Publish ในโหมด Release
RUN dotnet publish -c Release -o /app/publish --no-restore

# ============================================================
# Stage 2: Runtime Stage - ใช้ Nginx เพื่อเสิร์ฟแอป
# ============================================================
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# คัดลอกเฉพาะ Static Files ที่จำเป็นจาก Stage 1 มาใส่
COPY --from=build /app/publish/wwwroot .

# คัดลอกไฟล์ Config ของ Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# คัดลอกและตั้งค่า entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 80

# เปลี่ยนจาก CMD เป็น ENTRYPOINT เพื่อให้ script ของเราทำงานก่อน
ENTRYPOINT ["/entrypoint.sh"]
