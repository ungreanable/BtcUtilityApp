# ============================================================
# Stage 1: Build Stage - �� .NET SDK ���ͤ������ͻ
# ============================================================
# ����� .NET 8 SDK Image ����ͧ�Ѻ��� Publish ����ջ���Է���Ҿ
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# �Ѵ�͡��� .csproj ��зӡ�� Restore ��͹���������ª��ҡ Docker Caching
COPY *.csproj .
RUN dotnet restore

# �Ѵ�͡�����������ਡ��
COPY . .

# ����� Dynamic �¡��᷹��� base href ���ѵ��ѵ�
# ����觹�������¹ <base href="/" /> ��� <base href="__BASE_PATH__" />
RUN sed -i 's|<base href="/" />|<base href="__BASE_PATH__" />|' /src/wwwroot/index.html

# �ӡ�� Publish ����� Release
RUN dotnet publish -c Release -o /app/publish --no-restore

# ============================================================
# Stage 2: Runtime Stage - �� Nginx ����������ͻ
# ============================================================
# ����� Nginx �����ѹ Alpine ����բ�Ҵ�����л�ʹ����ҡ
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# �Ѵ�͡੾�� Static Files �����繨ҡ Stage 1 �����
COPY --from=build /app/publish/wwwroot .

# �Ѵ�͡��� Config �ͧ Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# �Ѵ�͡��е�駤�� entrypoint script
COPY entrypoint.sh /entrypoint.sh

# *** ���: ��������觹�������ŧ Line Endings �ҡ CRLF (Windows) �� LF (Linux) ***
# ��ͧ�ѹ�ѭ�� "no such file or directory" �͹�ѹ�� Linux/WSL2
RUN sed -i 's/\r$//' /entrypoint.sh

# ����� script ����ö execute ��
RUN chmod +x /entrypoint.sh

EXPOSE 80

# ����¹�ҡ CMD �� ENTRYPOINT ������� script �ͧ��ҷӧҹ��͹
ENTRYPOINT ["/entrypoint.sh"]
