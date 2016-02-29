using rfm69Base;

namespace rfmWebApi
{


    sealed class Switch
    {
        public int id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string houseCode { get; set; }
        public string plugCode { get; set; }
        public bool state { get; set; }

        public void On()
        {
            this.implementation().On();
            state = true;
        }

        public void Off()
        {
            this.implementation().Off();
            state = false;
        }

        private ISwitch implementation ()
        {
            if (type.Equals("brennenstuhl"))
                return new brennenstuhl(houseCode, plugCode);
            return new intertechno(houseCode, plugCode);
        }
    }
}
