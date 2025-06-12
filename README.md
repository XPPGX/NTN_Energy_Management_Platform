# NTN_Energy_Management_Platform

## Note
1. fastbuild.sh執行問題 : <br>
    Windows：結尾字符是 CRLF ("\r\n") <br>
    Linux：結尾字符是 LF ("\n") <br>
    把code從Windows轉移到Linux時，會因為這個預設結尾字符不同導致不能執行fastbuild.sh <br>
    <br>
    Solution : 將 .sh 檔案改為 LF結尾(可在vscode右下角改，改完後存檔即可)