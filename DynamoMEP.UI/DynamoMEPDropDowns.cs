using CoreNodeModels;
using Dynamo.Graph.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Newtonsoft.Json;


namespace DynamoMEP.UI
{
    [NodeName("FamilyInstanceReferenceType")]
    [NodeCategory("DynamoMEP.FamilyInstance")]
    [NodeDescription("Select the type of reference")]
    [IsDesignScriptCompatible]
    public class FamilyInstanceReferenceTypeDropDown : EnumBase<FamilyInstanceReferenceType>
    {
        public FamilyInstanceReferenceTypeDropDown()
        {
            var existing = OutPorts[0];
            OutPorts[0] = new PortModel(PortType.Output, this,
                new PortData(">", "The selected family reference type" , existing.DefaultValue));
            OutPorts[0].GUID = existing.GUID;
        }

        [JsonConstructor]
        public FamilyInstanceReferenceTypeDropDown(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            // TODO verify additional information for output ports is not required here!
        }

        protected override SelectionState PopulateItemsCore(string currentSelection)
        {
            //clear items
            Items.Clear();

            foreach (FamilyInstanceReferenceType familyInstanceReferenceType in Enum.GetValues(typeof(FamilyInstanceReferenceType)))
            {
                Items.Add(new DynamoDropDownItem(familyInstanceReferenceType.ToString(), familyInstanceReferenceType.ToString()));
            }

            Items = Items.OrderBy(x => x.Name).ToObservableCollection();
            SelectedIndex = 0;
            return SelectionState.Restore;
        }
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> { assign };
        }
    }
}
