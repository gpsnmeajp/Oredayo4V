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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityCipher;

/// <summary>
/// 簡易暗号化データ交換ライブラリ-クライアント側。
/// byte[]配列の交換を抽象化して行うためのライブラリです。
/// </summary>
public class SEDSS_Client : MonoBehaviour
{
    /// <summary>
    /// HTTPタイムアウト時間
    /// </summary>
    public int httpTimeout = 30;

    /// <summary>
    /// 通信先ポート
    /// </summary>
    public int port = 8000;

    /// <summary>
    /// 通信先アドレス
    /// </summary>
    string Address = "";

    /// <summary>
    /// 暗号化共通鍵
    /// </summary>
    string password = "";

    /// <summary>
    /// UTF-8(no BOM)定義
    /// </summary>
    readonly UTF8Encoding utf8 = new UTF8Encoding(false);

    /// <summary>
    /// 通信先アドレスを設定する
    /// </summary>
    /// <param name="Address">IPアドレスもしくはホスト名</param>
    public void SetAddress(string Address) {
        this.Address = Address;
    }

    /// <summary>
    /// 暗号化共通鍵を設定する
    /// </summary>
    /// <param name="password">共通鍵パスワード</param>
    public void SetPassword(string password)
    {
        this.password = password;
    }

    /// <summary>
    /// データを暗号化してアップロードする。IDはサーバー側識別用文字列。
    /// 成功すればOnOnSuccess、エラー発生時にはOnErrorがコールバックされる
    /// </summary>
    /// <param name="data">送信するバイナリデータ</param>
    /// <param name="id">サーバー側識別用文字列</param>
    /// <param name="OnSuccess">成功時コールバック(string: ID)</param>
    /// <param name="OnError">失敗時コールバック(string: 失敗理由, string: ID)</param>
    public void Upload(byte[] data,string id, Action<string> OnSuccess, Action<string,string> OnError)
    {
        StartCoroutine(UploadCoroutine(data, id, OnSuccess, OnError));
    }

    /// <summary>
    /// 暗号化されたデータをダウンロードする。IDはサーバー側識別用文字列。
    /// 成功すればOnOnSuccess、エラー発生時にはOnErrorがコールバックされる
    /// </summary>
    /// <param name="id">サーバー側識別用文字列</param>
    /// <param name="OnSuccess">成功時コールバック(byte[]:受信したバイナリデータ, string: ID)</param>
    /// <param name="OnError">失敗時コールバック(string: 失敗理由, string: ID)</param>
    public void Download(string id,Action<byte[],string> OnSuccess, Action<string, string> OnError)
    {
        StartCoroutine(RequestCoroutine(id,OnSuccess, OnError));
    }

    /// <summary>
    /// 暗号化アップロードコルーチン
    /// </summary>
    IEnumerator UploadCoroutine(byte[] data, string id, Action<string> OnSuccess, Action<string,string> OnError)
    {
        //IDが無の場合、仮文字列を入れる
        if (id == null || id == "") {
            id = "-";
        }

        //Dataが無の場合、処理しない
        if (data == null || data.Length == 0) {
            OnError?.Invoke("No data", id);
            yield break;
        }

        UnityWebRequest req;
        try
        {
            //URL組み立て
            string URL = "http://" + Address + ":" + port + "";

            //データを暗号化
            byte[] encryptedData = RijndaelEncryption.Encrypt(data, password);
            //IDを暗号化
            string encryptedId = RijndaelEncryption.Encrypt(id, password);

            //リクエストを生成
            req = UnityWebRequest.Put(URL + "/upload?"+encryptedId, encryptedData);
            req.timeout = httpTimeout;
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.ToString(),id);
            yield break;
        }

        //リクエストを送信して応答を待つ
        yield return req.SendWebRequest();

        try
        {
            //ネットワークエラー
            if (req.isNetworkError || req.isHttpError)
            {
                OnError?.Invoke(req.error, id);
                yield break;
            }
            //200 OKではない(パスワード誤りなど)
            if (req.responseCode != 200)
            {
                OnError?.Invoke("CODE:" + req.responseCode, id);
                yield break;
            }

            //レスポンスを復号
            byte[] response;
            try
            {
                response = RijndaelEncryption.Decrypt(req.downloadHandler.data, password);
            }
            catch (Exception) {
                //データ破損と思われる
                OnError?.Invoke("Decryption Error", id);
                yield break;
            }
            if (utf8.GetString(response) != "Upload OK") {
                //データ破損と思われる
                OnError?.Invoke("DecrtptFail", id);
                yield break;
            }

            //成功コールバックする
            OnSuccess?.Invoke(id);
            yield break;
        }
        catch (Exception e) {
            OnError?.Invoke(e.ToString(), id);
            yield break;
        }
    }

    /// <summary>
    /// 暗号化ダウンロードコルーチン
    /// </summary>
    IEnumerator RequestCoroutine(string id, Action<byte[],string> OnSuccess, Action<string, string> OnError)
    {
        //IDが無の場合、仮文字列を入れる
        if (id == null || id == "")
        {
            id = "-";
        }

        UnityWebRequest req;
        try
        {
            //URL組み立て
            string URL = "http://" + Address + ":" + port + "";

            //リクエスト(固定文字列)を暗号化
            string requestData = "request";
            byte[] keywordBytes = utf8.GetBytes(requestData);
            byte[] encryptedData = RijndaelEncryption.Encrypt(keywordBytes, password);

            //IDを暗号化
            string encryptedId = RijndaelEncryption.Encrypt(id, password);

            //リクエストを生成
            req = UnityWebRequest.Put(URL + "/request?"+ encryptedId, encryptedData);
            req.timeout = httpTimeout;
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.ToString(),id);
            yield break;
        }

        //リクエストを送信して応答を待つ
        yield return req.SendWebRequest();

        try
        {
            //ネットワークエラー
            if (req.isNetworkError || req.isHttpError)
            {
                OnError?.Invoke(req.error,id);
                yield break;
            }
            //200 OKではない(パスワード誤りなど)
            if (req.responseCode != 200)
            {
                OnError?.Invoke("CODE:" + req.responseCode,id);
                yield break;
            }

            //レスポンスを復号
            byte[] data;
            try
            {
                data = RijndaelEncryption.Decrypt(req.downloadHandler.data, password);
            }
            catch (Exception)
            {
                //データ破損と思われる
                OnError?.Invoke("Decryption Error", id);
                yield break;
            }
            OnSuccess?.Invoke(data, id);
            yield break;
        }
        catch (Exception e)
        {
            //成功コールバックする
            OnError?.Invoke(e.ToString(), id);
            yield break;
        }
    }
}
