using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioBaseCLIA.Run
{
    public static class DiuInfo
    {
        public static int[] diuCount = { 1,10,20,50,100,200,500 };
        public static string GetDiuInfo(int diuTime)
        {
            string DiuInfo = "";
            switch(diuTime)
            {
                case 1:
                case 10:
                case 20:
                    DiuInfo = diuTime.ToString();
                    break;
                case 50:
                    DiuInfo = "5;10";
                    break;
                case 100:
                    DiuInfo = "10;10";
                    break;
                case 200:
                    DiuInfo = "10;20";
                    break;
                case 500:
                    DiuInfo = "20;25";
                    break;
            }
            return DiuInfo;
        }
    }
}
