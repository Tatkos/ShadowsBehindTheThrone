﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Networking.NetworkSystem;

namespace Assets.Code
{
    public class PopupAutosaveDialog: MonoBehaviour
    {
        public Button bDismiss;
        public UIMaster ui;
        public Map map;
        public Text text;
        public bool hasSaved = false;

        public void Update()
        {
            if (!hasSaved)
            {
                try
                {
                    var info = new DirectoryInfo(".");
                    var fileInfo = info.GetFiles();
                    int maxAutosave = -1;
                    DateTime oldestAutosaveTime = DateTime.MaxValue;
                    int oldestAuto = -1;
                    foreach (FileInfo file in fileInfo)
                    {
                        if (file.Name.StartsWith("Autosave") && file.Name.EndsWith(".sv"))
                        {
                            string[] split = file.Name.Split('_');
                            if (split.Length == 3)
                            {
                                int saveInt = int.Parse(split[1]);
                                if (saveInt > maxAutosave)
                                {
                                    maxAutosave = saveInt;
                                }

                                if (file.LastWriteTime < oldestAutosaveTime)
                                {
                                    oldestAutosaveTime = file.LastWriteTime;
                                    oldestAuto = saveInt;
                                }
                            }
                        }
                    }

                    string filename = "Autosave_";
                    if (maxAutosave >= World.autosaveCount)
                    {
                        //We've used all our autosave slots
                        //Gotta backfill
                        filename += oldestAuto + "_.sv";
                    }
                    else
                    {
                        filename += (1+maxAutosave) + "_.sv";
                    }

                    World.staticMap.world.save(filename,false);
                    text.text = "Game Saved\nSaved as: " + filename;
                }
                catch (Exception e)
                {
                    text.text = "Save failed.\nReason: " + e.Message;
                }
                hasSaved = true;
            }
        }

        public void dismiss()
        {
            ui.removeBlocker(this.gameObject);
        }
    }
}