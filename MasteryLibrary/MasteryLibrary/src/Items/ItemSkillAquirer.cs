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
            if (byEntity.Api.Side == EnumAppSide.Server && byEntity is EntityPlayer player)
            {
                string? skillCode = Attributes?["skillCode"]?.AsString();
                if (string.IsNullOrEmpty(skillCode))
                {
                    player.Api.Logger.Error($"[MasteryLib] Item {Code} is missing 'skillCode' attribute.");
                    return;
                }


            }
        }
    }
}
