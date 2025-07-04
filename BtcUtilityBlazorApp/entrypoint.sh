#!/bin/sh

# ��駤�� default base path �� "/"
BASE_PATH=${ASPNETCORE_BASE_PATH:-/}
echo "Setting base path to: $BASE_PATH"

# �������᷹��� placeholder � index.html
# sed -i 's|__BASE_PATH__|'${BASE_PATH}'|g' /usr/share/nginx/html/index.html
# �� cp ��� sed ���ͤ�����ҡѹ����ա���
cp /usr/share/nginx/html/index.html /usr/share/nginx/html/index.html.tmp
sed 's|__BASE_PATH__|'${BASE_PATH}'|g' /usr/share/nginx/html/index.html.tmp > /usr/share/nginx/html/index.html

# �������÷ӧҹ�ͧ Nginx
echo "Starting Nginx..."
nginx -g 'daemon off;'
