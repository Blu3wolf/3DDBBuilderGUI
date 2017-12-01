using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3DDBBuilderGUI
{
    public class ObjDB
    {
        public ObjDB(string dir)
        {
            Dir = dir;
        }

        public string Dir { get; set; }
    }
}
