using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace DeviceConfigTools
{
    public class GatewayBindingModel : ViewModelBase
    {

        object _data;
        public object Data
        {
            get
            {
                if (_data == null)
                {
                    _data = GetData();
                }

                return _data;
            }
        }

        private object GetData()
        {
            throw new NotImplementedException();
        }


        EnumMemberViewModel _type;
        public EnumMemberViewModel Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (!object.Equals(_type, value))
                {
                    _type = value;

                    _data = null;

                    OnPropertyChanged("Type");
                    OnPropertyChanged("Data");
                }
            }
        }
    }
}
