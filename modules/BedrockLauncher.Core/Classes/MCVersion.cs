﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;
using System.Linq.Expressions;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using BedrockLauncher.Core.Components;
using System.Text.RegularExpressions;
using BedrockLauncher.Core.Interfaces;

namespace BedrockLauncher.Core.Classes
{
    public class MCVersion : NotifyPropertyChangedBase
    {
        public MCVersion(string uuid, string name, bool isBeta)
        {
            this.UUID = uuid.ToLower();
            this.Name = name;
            this.IsBeta = isBeta;
        }

        public string UUID { get; set; }
        public string Name { get; set; }
        public bool IsBeta { get; set; }
    }
}
