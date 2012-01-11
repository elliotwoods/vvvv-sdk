using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	interface IInstance
	{
		void Initialise();
		void Process();
		bool NeedsInitialise();
	}
}
