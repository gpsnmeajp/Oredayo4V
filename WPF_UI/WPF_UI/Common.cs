using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_UI
{
    class Common
    {
        [DefaultValue(false)]
        public bool Initialized { get; set; }
        [DefaultValue("")]
        public string SEDSSClientAddressTextBox_Text { get; set; }
        [DefaultValue("8000")]
        public string SEDSSClientPortTextBox_Text { get; set; }
        [DefaultValue("")]
        public string SEDSSClientPasswordTextBox_Password { get; set; }

    }
}
