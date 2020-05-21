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

[RequireComponent(typeof(SEDSS_Client))]
public class SEDSS_Client_Test : MonoBehaviour
{
    SEDSS_Client client;

    public bool upload = false;

    public string Address = "127.0.0.1";
    public string password = "1234";

    public string id = "";
    public string UploadData = "";
    public string DownloadData = "";
    public string Error = "";
    void Start()
    {
        client = GetComponent<SEDSS_Client>();
        client.SetAddress(Address);
        client.SetPassword(password);

        if (upload)
        {
            byte[] data = new UTF8Encoding(false).GetBytes(UploadData);
            Debug.Log(data.Length);
            client.Upload(data,id,(id) =>
            {
                Debug.Log("Upload OK ID:"+id);
            }, (e, id) =>
            {
                Error = e;
            });
        }
        else {
            Debug.Log("client.Download");
            client.Download(id,(data, id) => 
            {
                DownloadData = new UTF8Encoding(false).GetString(data);
                Debug.Log("Download OK ID:"+id);
            }, (e, id) => {
                Error = e;
            });
        }
    }
}
