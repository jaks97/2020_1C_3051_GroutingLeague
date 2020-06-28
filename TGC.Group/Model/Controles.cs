using Microsoft.DirectX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGC.Group.Model
{
    class Controles
    {
        public Key teclaAvanzar;
        public Key teclaRetroceder;
        public Key teclaIzquierda;
        public Key teclaDerecha;
        public Key teclaSalto;
        public Key teclaTurbo;

        public Controles(Key avanzar, Key retroceder, Key izquierda, Key derecha, Key salto, Key turbo)
        {
            teclaAvanzar = avanzar;
            teclaRetroceder = retroceder;
            teclaIzquierda = izquierda;
            teclaDerecha = derecha;
            teclaSalto = salto;
            teclaTurbo = turbo;
        }
    }
}
