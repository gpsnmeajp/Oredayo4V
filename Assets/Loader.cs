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

    public void LoadRequest(string path, Action<string, byte[]> callback) {
        byte[] bytes = File.ReadAllBytes(path);

        var context = new VRMImporterContext();
        context.ParseGlb(bytes);
        var meta = context.ReadMeta(true);

        GameObject modalObject = Instantiate(m_modalWindowPrefab, m_canvas.transform) as GameObject;
        var modalLocale = modalObject.GetComponentInChildren<VRMPreviewLocale>();
        modalLocale.SetLocale("ja");

        var modalUI = modalObject.GetComponentInChildren<VRMPreviewUI>();
        modalUI.setMeta(meta);
        modalUI.setLoadable(true);
        modalUI.m_ok.onClick.AddListener(()=> {
            Debug.Log("Licence Agreed");
            callback?.Invoke(path, bytes);
        });

        modalUI.m_cancel.onClick.AddListener(() => {
            Debug.Log("Licence disagreed");
        });
    }
}
