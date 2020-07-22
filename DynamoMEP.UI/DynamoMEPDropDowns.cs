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

namespace DynamoMEP.UI
{
    [NodeName("FamilyInstanceReferenceType")]
    [NodeCategory("DynamoMEP.FamillyInstance")]
    [NodeDescription("Select the type of reference")]
    [IsDesignScriptCompatible]
    public class CoordinateSystem : DSDropDownBase
    {
        public CoordinateSystem() : base(">") { }

        protected override SelectionState PopulateItemsCore(string currentSelection)
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(typeof(FamilyInstanceReferenceType)))
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
            return SelectionState.Done;
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
