# ============================================================
# Stage 1: Build Stage - ใช้ .NET SDK เพื่อคอมไพล์แอป
# ============================================================
# เราใช้ .NET 8 SDK Image ซึ่งรองรับการ Publish ที่มีประสิทธิภาพ
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# คัดลอกไฟล์ .csproj และทำการ Restore ก่อนเพื่อใช้ประโยชน์จาก Docker Caching
COPY *.csproj .
RUN dotnet restore

# คัดลอกไฟล์ทั้งหมดในโปรเจกต์
COPY . .

# ทำให้ Dynamic โดยการแทนที่ base href โดยอัตโนมัติ
# คำสั่งนี้จะเปลี่ยน <base href="/" /> ไปเป็น <base href="__BASE_PATH__" />
RUN sed -i 's|<base href="/" />|<base href="__BASE_PATH__" />|' /src/wwwroot/index.html

# ทำการ Publish ในโหมด Release
RUN dotnet publish -c Release -o /app/publish --no-restore

# ============================================================
# Stage 2: Runtime Stage - ใช้ Nginx เพื่อเสิร์ฟแอป
# ============================================================
# เราใช้ Nginx เวอร์ชัน Alpine ซึ่งมีขนาดเล็กและปลอดภัยมาก
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# คัดลอกเฉพาะ Static Files ที่จำเป็นจาก Stage 1 มาใส่
COPY --from=build /app/publish/wwwroot .

# คัดลอกไฟล์ Config ของ Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# คัดลอกและตั้งค่า entrypoint script
COPY entrypoint.sh /entrypoint.sh

# *** แก้ไข: เพิ่มคำสั่งนี้เพื่อแปลง Line Endings จาก CRLF (Windows) เป็น LF (Linux) ***
# ป้องกันปัญหา "no such file or directory" ตอนรันบน Linux/WSL2
RUN sed -i 's/\r$//' /entrypoint.sh

# ทำให้ script สามารถ execute ได้
RUN chmod +x /entrypoint.sh

EXPOSE 80

# เปลี่ยนจาก CMD เป็น ENTRYPOINT เพื่อให้ script ของเราทำงานก่อน
ENTRYPOINT ["/entrypoint.sh"]
