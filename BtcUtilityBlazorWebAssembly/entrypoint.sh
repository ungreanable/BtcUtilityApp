#!/bin/sh

# ��駤�� default base path �� "/"
# ���ź trailing slash ����Ҩ������ (�������� root) ���ͤ����١��ͧ
BASE_PATH=$(echo "${ASPNETCORE_BASE_PATH:-/}" | sed 's:/*$::')
# ����Ѻ Nginx ��ҵ�ͧ��� path ����� / ��ͷ��� (¡��� root)
NGINX_BASE_PATH="${BASE_PATH}/"

echo "Base Path for Blazor (<base href>): ${NGINX_BASE_PATH}"
echo "Base Path for Nginx (location): ${NGINX_BASE_PATH}"

# ᷹��� Placeholder � index.html
# �� | �� delimiter ���ͻ�ͧ�ѹ�ѭ�ҡѺ����ͧ���� / � path
sed -i 's|__BASE_PATH__|'${NGINX_BASE_PATH}'|g' /usr/share/nginx/html/index.html

# ᷹��� Placeholder � nginx.conf
sed -i 's|__NGINX_BASE_PATH__|'${NGINX_BASE_PATH}'|g' /etc/nginx/conf.d/default.conf

# �������÷ӧҹ�ͧ Nginx
echo "Starting Nginx..."
nginx -g 'daemon off;'