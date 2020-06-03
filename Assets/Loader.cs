using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRM;
using VRMLoader;
using EVMC4U;

public class Loader : MonoBehaviour
{
    public Canvas m_canvas;
    public GameObject m_modalWindowPrefab;
    private VRMPreviewUI modalUI = null;
    private Action<string, byte[]> callbackHandler = null;
    private byte[] VRMdata = null;
    private string VRMpath = null;

    public void LoadRequest(string path, Action<string, byte[]> callback) {
        byte[] bytes = File.ReadAllBytes(path);

        var context = new VRMImporterContext();
        context.ParseGlb(bytes);
        var meta = context.ReadMeta(true);

        GameObject modalObject = Instantiate(m_modalWindowPrefab, m_canvas.transform) as GameObject;
        var modalLocale = modalObject.GetComponentInChildren<VRMPreviewLocale>();
        modalLocale.SetLocale("ja");

        if (modalUI != null)
        {
            modalUI.destroyMe();
            modalUI = null;
        }

        modalUI = modalObject.GetComponentInChildren<VRMPreviewUI>();
        modalUI.setMeta(meta);
        modalUI.setLoadable(true);

        callbackHandler = callback;
        VRMdata = bytes;
        VRMpath = path;
    }

    public void Agree()
    {
        modalUI.destroyMe();
        modalUI = null;

        callbackHandler?.Invoke(VRMpath, VRMdata);
    }

    public void DisAgree()
    {
        modalUI.destroyMe();
        modalUI = null;
    }
}
