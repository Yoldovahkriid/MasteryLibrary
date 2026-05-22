using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

namespace MasteryLibrary.src.Items
{
    public class ItemSkillAquirer: Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }
    }
}
