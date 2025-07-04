#!/bin/sh

# ตั้งค่า default base path เป็น "/"
# และลบ trailing slash ที่อาจมีอยู่ (ถ้าไม่ใช่ root) เพื่อความถูกต้อง
BASE_PATH=$(echo "${ASPNETCORE_BASE_PATH:-/}" | sed 's:/*$::')
# สำหรับ Nginx เราต้องการ path ที่มี / ต่อท้าย (ยกเว้น root)
NGINX_BASE_PATH="${BASE_PATH}/"

echo "Base Path for Blazor (<base href>): ${NGINX_BASE_PATH}"
echo "Base Path for Nginx (location): ${NGINX_BASE_PATH}"

# แทนที่ Placeholder ใน index.html
# ใช้ | เป็น delimiter เพื่อป้องกันปัญหากับเครื่องหมาย / ใน path
sed -i 's|__BASE_PATH__|'${NGINX_BASE_PATH}'|g' /usr/share/nginx/html/index.html

# แทนที่ Placeholder ใน nginx.conf
sed -i 's|__NGINX_BASE_PATH__|'${NGINX_BASE_PATH}'|g' /etc/nginx/conf.d/default.conf

# เริ่มการทำงานของ Nginx
echo "Starting Nginx..."
nginx -g 'daemon off;'