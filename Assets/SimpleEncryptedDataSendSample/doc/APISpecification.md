# API仕様
簡易暗号化データ交換ライブラリ

byte[]配列の交換を抽象化して行うための手軽なライブラリです。  
Unity間で、byte[]配列1つ渡せればそれでいいのになー、面倒だなー、って時に使えます。  
ファイルを経由せず、メモリ上で送受信します。  

送受信側双方でパスワードが一致していないと通信が成立しない(復号できない)ため、簡単ないたずら防止に利用できます。  
内部的にはHTTPで通信しているため、サーバーのポートが空いていればそれ以上は特に必要ありません。  

スマートフォン・PC間での通信や、LAN内の複数台のPCで使えます。

VMCProtocolでモーションデータを送る時に、VRMファイルを同期させるための用途が一番の利用想定です。

注意: 暗号化処理はAES256ですが、通信プロトコルは簡易的なものです。  
インターネット上での利用を想定した作りにはなっていません。  
あくまでLAN内、VPN上などでの利用を想定しています。

# SEDSS_Client
MonoBehaviourのため、適当なGameObjectにアタッチして使用します。

スクリプトから以下のように呼び出すことで、簡単にアップロード・ダウンロードができます。

## サンプルコード
```cs
    void upload()
    {
        var client = GetComponent<SEDSS_Client>();
        client.SetAddress("127.0.0.1");
        client.SetPassword("1234");

        string request_id = "test message";
        byte[] data = new System.Text.UTF8Encoding(false).GetBytes("Hello World");

        Debug.Log("Upload Start ID:" + request_id);
        client.Upload(data, request_id, (id) =>
        {
            Debug.Log("Upload OK ID:" + id);
        }, (e, id) => {
            Debug.Log("Upload Error ID:" + id);
            Debug.Log("Message:" + e);
        });
    }
```
```cs
    void download()
    {
        var client = GetComponent<SEDSS_Client>();
        client.SetAddress("127.0.0.1");
        client.SetPassword("1234");

        string request_id = "test message";
        Debug.Log("Download Start ID:" + request_id);
        client.Download(request_id, (data, id) =>
        {
            Debug.Log("Download OK ID:" + id);
            Debug.Log("Message:" + new System.Text.UTF8Encoding(false).GetString(data));
        }, (e, id) => {
            Debug.Log("Download Error ID:" + id);
            Debug.Log("Message:" + e);
        });
    }
```

## httpTimeout
HTTPタイムアウト値(秒)です。既定で30秒です。  
巨大なファイルを受け取るときなどは長めにしてください。

## port
HTTPポートです。サーバー側で提示されているものを指定してください。

## void SetAddress(string Address)
アクセス先のサーバーのIPアドレスまたはホストを設定します。

## void SetPassword(string password)
暗号化に使用するパスワードを設定します。十分長いものを設定してください。

## void Upload(byte[] data,string id, Action<string> OnSuccess, Action<string,string> OnError)
アップロードを実行します。指定したデータ、IDはサーバーに伝達されます。  
IDによってどう振る舞うかは、サーバー側の応答の実装に依存します。(簡易な場合は無視します)  
サイズ0のデータは送信できません。データサイズは1GB以下を想定しています。  

通信成功すると、OnSuccess(id)コールバックが発生します。  
通信失敗すると、OnError(error,id)コールバックが発生します。

## void Download(string id,Action<byte[],string> OnSuccess, Action<string, string> OnError)
ダウンロードを実行します。指定したIDはサーバーに伝達されます。  
IDによってどう振る舞うかは、サーバー側の応答の実装に依存します。(簡易な場合は無視します)  

通信成功すると、OnSuccess(data,id)コールバックが発生します。  
通信失敗すると、OnError(error,id)コールバックが発生します。

# SEDSS_Server
MonoBehaviourのため、適当なGameObjectにアタッチして使用します。
## サンプルコード
```cs
    void Start()
    {
        var server = GetComponent<SEDSS_Server>();
        server.SetPassword("1234");
        server.StartServer();

        server.OnDataUploaded = (data, id) => {
            Debug.Log("Server Received ID:" + id);
            Debug.Log("Message:" + new System.Text.UTF8Encoding(false).GetString(data));
        };
        server.OnDownloadRequest = (id) => {
            Debug.Log("Server Send ID:" + id);
            return new System.Text.UTF8Encoding(false).GetBytes("You're welcome");
        };
    }
```

## domain
HTTP Listener ドメイン(ホスト名)指定  
(*=Any, インターネットに向けて公開する場合はホスト名を設定することが、RFC7230に基づきMS公式より強く推奨されている)

## port
HTTP待受ポートです。

## void SetPassword(string password)
暗号化に使用するパスワードを設定します。十分長いものを設定してください。

## void StartServer()
サーバーを起動します。

## void StopServer()
サーバーを停止します。

## Action<byte[], string> OnDataUploaded;
クライアントからアップロードを受け付けたときのコールバックです。  
サーバーを開始する前に登録しておくことをおすすめします。  
(byte[]: data, string: id)

クライアントがデータをアップロードしてきた時に呼び出されます。  
IDをどう処理するかは自由です。return後に応答が帰ります。throwすると400エラーになります。  

**この処理はメインスレッドではありません。Unityの関数などは呼ぶことができません。**  
**予め用意してあるデータを渡す処理にとどめてください。**  

処理する時間が長いとクライアントがタイムアウトすることがあります。


## Func<string, byte[]> OnDownloadRequest;
クライアントからダウンロード要求を受け付けたときのコールバックです。  
サーバーを開始する前に登録しておくことをおすすめします。  
(return byte[]: data, string: id)

クライアントがダウンロードを要求してきた時に呼び出されます。  
IDをどう処理するかは自由です。データをreturn後に応答が帰ります。

**この処理はメインスレッドです。Unityの関数などを呼ぶことができます。**  

処理する時間が長いとクライアントがタイムアウトすることがあります。
