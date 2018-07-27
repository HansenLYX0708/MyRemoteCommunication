using System;
using System.Collections.Generic;
using System.Text;

namespace WD.Tester.Module
{
    public class ServoPositionClass
    {
        public ServoPositionClass()
        {
            position = 0;
            velocity = 0;
            acceleration = 0;
        }

        public int position;
        public int velocity;
        public int acceleration;
    }
}
