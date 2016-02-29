using System.Collections.Generic;

namespace rfmWebApi
{
    static class SwitchList
    {
        static List<Switch> switchList;

        internal static Switch contains (int id)
        {
            foreach (Switch sw in switchList)
            {
                if (id == sw.id)
                    return sw;
            }
            return null;
        }

        internal static List<Switch> list ()
        {
            return switchList;
        }

        static SwitchList()
        {
            switchList = new List<Switch>();

            switchList.Add(new Switch()
            {
                id = 0,
                name = "Schalter 1",
                type = "brennenstuhl",
                houseCode = "01001",
                plugCode = "01111",
                state = false
            });
            switchList.Add(new Switch()
            {
                id = 1,
                name = "Schalter 2",
                type = "intertechno",
                houseCode = "F0F0",
                plugCode = "F000",
                state = false
            });
        } 
    }
}
