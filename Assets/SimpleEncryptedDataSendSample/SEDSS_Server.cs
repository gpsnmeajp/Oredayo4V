/*
MIT License

Copyright (c) 2020 gpsnmeajp

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityCipher;


/// <summary>
/// 簡易暗号化データ交換ライブラリ-サーバー側。
/// byte[]配列の交換を抽象化して行うためのライブラリです。
/// </summary>
public class SEDSS_Server : MonoBehaviour
{
    /// <summary>
    /// HTTP Listener ドメイン(ホスト名)指定
    /// (*=Any, インターネットに向けて公開する場合はホスト名を設定することがMS公式より強く推奨されている)
    /// </summary>
    public string domain = "*";

    /// <summary>
    /// HTTP ポート
    /// </summary>
    public int port = 8000;

    /// <summary>
    /// クライアントからアップロードを受け付けたときのコールバック
    /// (byte[]: data, string: id)
    /// </summary>
    public Action<byte[], string> OnDataUploaded = null;

    /// <summary>
    /// クライアントからダウンロード要求を受け付けたときのコールバック
    /// (return byte[]: data, string: id)
    /// </summary>
    public Func<string, byte[]> OnDownloadRequest = null;

    /// <summary>
    /// 暗号化共通鍵
    /// </summary>
    string password = null;

    /// <summary>
    /// UTF-8(no BOM)定義
    /// </summary>
    readonly UTF8Encoding utf8 = new UTF8Encoding(false);

    /// <summary>
    /// HTTPListener
    /// </summary>
    HttpListener listener;

    /// <summary>
    /// HTTPListener Thread
    /// </summary>
    Thread thread = null;

    /// <summary>
    /// Thread Context(メインスレッド)
    /// </summary>
    SynchronizationContext MainThreadContext;

    /// <summary>
    /// 暗号化共通鍵を設定する
    /// </summary>
    /// <param name="password">共通鍵パスワード</param>
    public void SetPassword(string password)
    {
        this.password = password;
    }

    /// <summary>
    /// MonoBehaviour Start
    /// </summary>
    void Start()
    {
        MainThreadContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// サーバー起動
    /// </summary>
    public void StartServer()
    {
        //サーバー起動
        listener = new HttpListener();
        listener.Prefixes.Add("http://" + domain + ":" + port + "/");
        listener.Start();

        //受信処理スレッド起動
        thread = new Thread(new ThreadStart(ReceiveThread));
        thread.Start();
    }

    /// <summary>
    /// サーバー停止
    /// </summary>
    public void StopServer()
    {
        try
        {
            listener?.Close();
        }
        catch (Exception e)
        {
            //Do noting
        }
        finally
        {
            thread?.Join();
        }
    }

    /// <summary>
    /// MonoBehaviour OnDestroy
    /// </summary>
    private void OnDestroy()
    {
        StopServer();
    }

    /// <summary>
    /// 受信処理スレッド
    /// </summary>
    private void ReceiveThread()
    {
        try
        {
            //サーバーがリッスン状態かチェック(でなければ終了)
            while (listener.IsListening)
            {
                //リクエストを受信
                HttpListenerContext context = listener?.GetContext();
                HttpListenerRequest request = context?.Request;
                HttpListenerResponse response = context?.Response;

                //リクエストが無効(終了時など)
                if (context == null || request == null || request == null) {
                    //適度な待ち時間
                    Thread.Sleep(30);
                    continue;
                }

                //初期値は400 Bad Request(セキュリティを鑑み、異常値はこれだけ返す)
                response.StatusCode = 400;
                byte[] res = utf8.GetBytes("400 Bad Request");

                //例外時にも一応の応答を返却するため、tryブロックにしている
                try
                {
                    //PUTのみ受け付ける、かつ、データが存在する
                    //(クライアント側のUnityWebRequestがPUTだと既定でoctet-streamを扱ってくれるため)
                    if (request.HttpMethod == "PUT" && request.HasEntityBody)
                    {
                        //クエリ文字列をIDとして復号する(必ず存在する)
                        //復号できない場合は、データが壊れているか、パスワードが間違っている
                        string id;
                        try
                        {
                            id = RijndaelEncryption.Decrypt(request.Url.Query.Remove(0, 1), password);
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException("Decryption Error on ID");
                        }

                        //データを受信する
                        long len = request.ContentLength64; //データ長
                        byte[] rcvBuf; //データ受信バッファ
                        byte[] membuf = new byte[256]; //一時受信用バッファ

                        //Stramから受信するため、MemoryStreamを使う
                        using (var memoryStream = new MemoryStream())
                        {
                            //取り出しループ
                            while (true)
                            {
                                //1回の読み出しではすべて出てこないことがある
                                int readlen = request.InputStream.Read(membuf, 0, membuf.Length);
                                if (readlen <= 0)
                                {
                                    break;
                                }
                                memoryStream.Write(membuf, 0, readlen);
                            }
                            //MemoryStreamからbyte[]として取り出し
                            rcvBuf = memoryStream.ToArray();
                        }

                        //復号を試す
                        //復号できない場合は、データが壊れているか、パスワードが間違っている
                        byte[] decryptedReceiveData;
                        try
                        {
                            decryptedReceiveData = RijndaelEncryption.Decrypt(rcvBuf, password);
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException("Decryption Error on DATA");
                        }

                        //パスに基づき処理する(パスは生文字列のコマンド)
                        switch (request.Url.LocalPath)
                        {
                            //ダウンロード要求
                            case "/request":
                                {
                                    //復号されたデータが、コマンド文字列と合うかチェック
                                    //これによりパスワードが合っているかをチェックしている
                                    if (utf8.GetString(decryptedReceiveData) == "request")
                                    {
                                        //問題ないため、200 OK
                                        response.StatusCode = 200;

                                        //ダウンロード要求コールバックに投げて、データを貰う
                                        byte[] data = OnDownloadRequest?.Invoke(id);

                                        //応答データを暗号化して送信準備
                                        res = RijndaelEncryption.Encrypt(data, password);
                                    }
                                }
                                break;
                            //アップロード要求
                            case "/upload":
                                {
                                    //復号されたデータをアップロード時コールバックに渡す
                                    MainThreadContext.Post((state) =>
                                    {
                                        OnDataUploaded?.Invoke(decryptedReceiveData, id);
                                    }, null);

                                    //問題ないため、200 OK
                                    response.StatusCode = 200;

                                    //応答データを暗号化して送信準備
                                    string responseString = ("Upload OK"); //固定文字列
                                    res = RijndaelEncryption.Encrypt(utf8.GetBytes(responseString), password);
                                }
                                break;
                            default:
                                //Bad request
                                break;
                        }
                    }
                }
                catch (ObjectDisposedException) {
                    //強制終了された
                    return;
                }

                catch (Exception e)
                {
                    //異常は記録する
                    Debug.Log(e);

                    //異常なリクエストということにする
                    response.StatusCode = 400;
                    res = utf8.GetBytes("400 Bad Request");
                }

                //データを送信する
                response.OutputStream.Write(res, 0, res.Length);
                //通信を終了する
                response.OutputStream.Close();

                //適度な待ち時間
                Thread.Sleep(30);
            }
        }
        catch (HttpListenerException)
        {
            //Do noting
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}

