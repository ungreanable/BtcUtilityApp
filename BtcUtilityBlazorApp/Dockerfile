# ============================================================
# Stage 1: Build Stage - ใช้ .NET SDK เพื่อคอมไพล์แอป
# ============================================================
# เราใช้ .NET 8 SDK Image ซึ่งรองรับการ Publish ที่มีประสิทธิภาพ
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# คัดลอกไฟล์ .csproj และทำการ Restore ก่อนเพื่อใช้ประโยชน์จาก Docker Caching
COPY *.csproj .
RUN dotnet restore

# คัดลอกไฟล์ทั้งหมดในโปรเจกต์ แล้วทำการ Publish ในโหมด Release
# ผลลัพธ์ที่ได้คือ Static Files ที่พร้อมใช้งานในโฟลเดอร์ /app/publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ============================================================
# Stage 2: Runtime Stage - ใช้ Nginx เพื่อเสิร์ฟแอป
# ============================================================
# เราใช้ Nginx เวอร์ชัน Alpine ซึ่งมีขนาดเล็กและปลอดภัยมาก
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# คัดลอกเฉพาะ Static Files ที่จำเป็นจาก Stage 1 มาใส่
COPY --from=build /app/publish/wwwroot .

# คัดลอกไฟล์ Config ของ Nginx เพื่อจัดการเรื่อง Routing ของ Blazor
COPY nginx.conf /etc/nginx/conf.d/default.conf

# เปิด Port 80 ของ Container
EXPOSE 80

# สั่งให้ Nginx ทำงานเมื่อ Container เริ่มต้น
CMD ["nginx", "-g", "daemon off;"]
