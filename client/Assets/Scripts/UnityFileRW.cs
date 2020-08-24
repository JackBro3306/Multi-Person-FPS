using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UnityFileRW : MonoBehaviour {
    
    private static string GetBasePath() {
        string pathBase = "";
        switch (Application.platform) {
            case RuntimePlatform.Android:
                pathBase = Application.persistentDataPath;
                break;
            case RuntimePlatform.WindowsPlayer:
                pathBase = Application.dataPath;
                break;
            case RuntimePlatform.WindowsEditor:
                pathBase = Application.dataPath;
                break;
        }
        return pathBase;
    }
	
    /// <summary>
    /// 在指定位置创建文件，如果文件已经存在则追加文件内容
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="name">文件名</param>
    /// <param name="info">文件内容</param>
	public void CreateORwriteFile(string name,string info) {
        string path = GetBasePath();

        StreamWriter sw;
        FileInfo fInfo = new FileInfo(path+"//"+name);
        if (!fInfo.Exists) {
            sw = fInfo.CreateText();
        } else {
            sw = fInfo.AppendText();
        }

        sw.WriteLine(info);
        sw.Close();
        sw.Dispose();
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="name">文件名</param>
    public void DeleteFile(string name) {
        string path = GetBasePath();
        File.Delete(path+"//"+name);
    }

    /// <summary>
    /// 读取文件，只读取第一行
    /// </summary>
    /// <param name="path"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string LoadFile(string name) {
        string path = GetBasePath();
        FileInfo fInfo = new FileInfo(path + "//" + name);
        if (!fInfo.Exists) {
            return "error";
        }
        StreamReader sr = null;
        sr = File.OpenText(path + "//" + name);
        string line = sr.ReadLine();
        
        sr.Close();
        sr.Dispose();
        return line;
    } 
}
