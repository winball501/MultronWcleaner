﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MultronWinCleaner
{
    class themeselector
    {
        public static void selector(Uri uri)
        {

            ResourceDictionary theme = new ResourceDictionary()
            {
                Source = uri,
            };

            App.Current.Resources.Clear();
            App.Current.Resources.MergedDictionaries.Add(theme);
         

        }
    }
}
