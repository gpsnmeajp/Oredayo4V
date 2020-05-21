# SimpleEncryptedDataSendSample
簡単に暗号化してUnityでデータを送受信するC#スクリプトライブラリ。  

byte[]配列の交換を抽象化して行うための手軽なライブラリです。  
ファイルを経由せず、メモリ上で送受信します。  

内部ではHTTPを利用していますが、利用する際は意識せずに利用できます。  
Unity間で、byte[]配列1つ渡せればそれでいいのになー、サーバーとか通信とか面倒だなー、って時に使えます。  

このサンプルのセキュリティは、いたずら防止程度のものと考えてください。  
LAN内やVPN上で使用する前提です。

詳細な仕様は、**[APIドキュメント](https://github.com/gpsnmeajp/SimpleEncryptedDataSendSample/blob/master/doc/APISpecification.md)** を確認してください。

動作にはUnityCipher (暗号化ライブラリ)が必要です  
https://github.com/TakuKobayashi/UnityCipher

<img src="https://github.com/gpsnmeajp/SimpleEncryptedDataSendSample/blob/master/doc/SEDSS.png?raw=true"></img>

# ライセンス
MIT Licence

# [お問合せ先(Discordサーバー)](https://discord.gg/nGapSR7)
