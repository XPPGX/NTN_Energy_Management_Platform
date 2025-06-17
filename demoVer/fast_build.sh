#!/bin/bash
set -e

export DOTNET_ROOT=/home/linaro/.dotnet
export PATH=$DOTNET_ROOT:$PATH
VERSION="1.0.0" 
##################################################################
APP_NAME="NTN-aarch64.AppImage"
APP_DIR="/home/linaro/Remote_Repo"
SRC_DIR="/home/linaro/test_demo2/NTN_Energy_Management_Platform/demoVer"
##################################################################
PUBLISH_DIR="$SRC_DIR/publish"
TEMP_NAME="NEW_$APP_NAME"

echo "[Step 1] 防止Razor或Shared Compiler卡住..."
dotnet build -c Release /p:UseRazorBuildServer=false /p:UseSharedCompilation=false

echo "[Step 2] 關閉正在執行的 AppImage（若有）..."
pkill -f demoVer.dll && echo "[Info] AppImage 已停止" || echo "[Info] 沒有運行中程序"

echo "[Step 3] 編譯專案..."
cd "$SRC_DIR"
rm -rf "$PUBLISH_DIR"

dotnet clean
dotnet publish -c Release -o "$PUBLISH_DIR" -p:PublishWebConfigFile=false

echo "[Step 4] 打包 AppImage..."
cd "$PUBLISH_DIR"

# 複製必要檔案
mkdir -p usr/share
echo "$VERSION" > usr/share/version.txt



# 建立 AppRun
cat > AppRun << 'EOF'
#!/bin/bash
export DOTNET_ROOT=/home/linaro/.dotnet
export PATH=/home/linaro/.dotnet:$PATH
export ASPNETCORE_URLS=http://0.0.0.0:5042
cd "$(dirname "$0")"
exec /home/linaro/.dotnet/dotnet demoVer.dll
EOF

chmod +x AppRun

# 建立 desktop 檔
cat > NTN.desktop << 'EOF'
[Desktop Entry]
Type=Application
Name=NTN
Exec=AppRun
Icon=placeholder
Categories=Utility;
EOF

# 加入圖示
mkdir -p usr/share/icons/hicolor/128x128/apps/
printf '\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01\x08\x02\x00\x00\x00\x90wS\xde\x00\x00\x00\nIDATx\xdacd\xf8\x0f\x00\x01\x01\x01\x00\x18\xdd\x8d\xa0\x00\x00\x00\x00IEND\xaeB`\x82' > usr/share/icons/hicolor/128x128/apps/placeholder.png

# 打包 AppImage
linuxdeploy --appdir . --desktop-file=NTN.desktop --output appimage


echo "[Step 5] 複製到目標資料夾，暫存為 $TEMP_NAME..."
cp "$APP_NAME" "$APP_DIR/$TEMP_NAME"


echo "[Step 5-1] 再次確認沒有執行中的 AppImage..."
pkill -f demoVer.dll && sleep 1

echo "[Step 5-2] 移除舊 AppImage 並替換..."
rm -f "$APP_DIR/$APP_NAME"
mv "$APP_DIR/$TEMP_NAME" "$APP_DIR/$APP_NAME"

chown linaro:linaro "$APP_DIR/$APP_NAME"
chmod 755 "$APP_DIR/$APP_NAME"

echo "[✔ Done] 已完成更新與替換：$APP_NAME"
