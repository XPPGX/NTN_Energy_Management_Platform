# NTN_Energy_Management_Platform


1. fastbuild.sh執行問題 : <br>
    Windows：結尾字符是 CRLF ("\r\n") <br>
    Linux：結尾字符是 LF ("\n") <br>
    把code從Windows轉移到Linux時，會因為這個預設結尾字符不同導致不能執行fastbuild.sh <br>
    <br>
    Solution : 將 .sh 檔案改為 LF結尾(可在vscode右下角改，改完後存檔即可)
2. 檔案結構 =>
    - "Components/Pages"：底下的檔案都是可經由TopRow切換的分頁
    - "Components/Shares"：底下的檔案是可被父razor引用的子razor
    - "Models/"：底下都是資料結構的定義
    - "Services/"：底下都是定義服務的檔案
    - "Utils/"：底下都是可被全檔案使用的純計算類function
## Note
1. "global.json"：該檔案用途是在Windows上開發時指定dotnet版本，若將專案放在linux上跑，則這個global.json要刪掉(即可回到linux指定dotnet版本)