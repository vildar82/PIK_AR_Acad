using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(PIK_AR_Acad.Commands))]

namespace PIK_AR_Acad
{
    public class Commands
    {
        public const string GroupPIK = AcadLib.Commands.Group;

        [CommandMethod(GroupPIK, nameof(AI_ApartmentsTypology), CommandFlags.Modal)]
        public void AI_ApartmentsTypology ()
        {
            AcadLib.CommandStart.Start(doc =>
            {
                var topologyApartmets = new Interior.Typology.ApartmentTypology (doc);
                topologyApartmets.CreateTableTypology();
            });
        }
    }
}
