#!/bin/sh

# ตั้งค่า default base path เป็น "/"
BASE_PATH=${ASPNETCORE_BASE_PATH:-/}
echo "Setting base path to: $BASE_PATH"

# ค้นหาและแทนที่ placeholder ใน index.html
# sed -i 's|__BASE_PATH__|'${BASE_PATH}'|g' /usr/share/nginx/html/index.html
# ใช้ cp และ sed เพื่อความเข้ากันได้ที่ดีกว่า
cp /usr/share/nginx/html/index.html /usr/share/nginx/html/index.html.tmp
sed 's|__BASE_PATH__|'${BASE_PATH}'|g' /usr/share/nginx/html/index.html.tmp > /usr/share/nginx/html/index.html

# เริ่มการทำงานของ Nginx
echo "Starting Nginx..."
nginx -g 'daemon off;'
