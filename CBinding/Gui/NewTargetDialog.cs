﻿using AppKit;
using System;
namespace CBinding
{
    public class NewTargetDialog : NSViewController
    {
        public CMakeTarget.Types TargetType { get; set; }
        public string TargetName { get; set; }

        public NewTargetDialog ()
        {
            //buttonOk.Clicked += ButtonOkClicked;
        }

        void ButtonOkClicked (object sender, EventArgs e)
        {
            //TargetName = targetName.Text;
            //switch (targetType.ActiveText.ToLower ()) {
            //case "binary":
            //    TargetType = CMakeTarget.Types.Binary;
            //    break;
            //case "static library":
            //    TargetType = CMakeTarget.Types.StaticLibrary;
            //    break;
            //case "shared library":
            //    TargetType = CMakeTarget.Types.SharedLibrary;
            //    break;
            //case "module":
            //    TargetType = CMakeTarget.Types.Module;
            //    break;
            //case "object":
            //    TargetType = CMakeTarget.Types.ObjectLibrary;
            //    break;
            //}
        }
    }
}

