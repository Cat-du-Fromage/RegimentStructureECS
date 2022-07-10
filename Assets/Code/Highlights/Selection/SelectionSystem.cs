using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(GroupPreselectionSystem))]
    public partial class SelectionSystem : SystemBase
    {
        private EntityQuery regimentQuery;
        
        private ButtonControl leftMouseClick;
        private KeyControl leftShift;

        protected override void OnCreate()
        {
            regimentQuery = GetEntityQuery(ComponentType.ReadOnly<Tag_Regiment>());
        }

        protected override void OnStartRunning()
        {
            leftMouseClick = Mouse.current.leftButton;
            leftShift = Keyboard.current.leftShiftKey;
        }

        protected override void OnUpdate()
        {
            if (!leftMouseClick.wasReleasedThisFrame) return;
            /*
            Entities
            .WithName("Selection_Regiment_SetFilterChange")
            .WithBurst()
            .WithAll<Tag_Regiment>()
            .ForEach((Entity regimentEntity, in Flag_Preselection preselectFlag) =>
            {
                bool isPreselected = preselectFlag.IsActive;
                bool isSelected = GetComponent<Flag_Selection>(regimentEntity).IsActive;
                if ((!isPreselected && !isSelected) || (isPreselected && isSelected)) return;
                SetComponent(regimentEntity, new Flag_Selection(){IsActive = isPreselected});
            }).Run();
            */
            //ADD CONDITION FOR LEFT MAJ
            if (leftShift.isPressed)
            {
                SelectionOnly();
            }
            else
            {
                SelectAndDeselect();
            }
        }

        private void SelectionOnly()
        {
            Entities
            .WithName("SelectionOnly_Regiment_SetFilterChange")
            .WithBurst()
            .WithAll<Tag_Regiment>()
            .ForEach((Entity regimentEntity, in Flag_Preselection preselectFlag) =>
            {
                bool isPreselected = preselectFlag.IsActive;
                bool isSelected = GetComponent<Flag_Selection>(regimentEntity).IsActive;
                if (!isPreselected || isSelected) return;
                SetComponent(regimentEntity, new Flag_Selection(){IsActive = true});
                SetComponent(regimentEntity, new Filter_Selection(){DidChange = true});
            }).Run();
        }

        private void SelectAndDeselect()
        {
            Entities
            .WithName("SelectAndDeselect_Regiment_SetFilterChange")
            .WithBurst()
            .WithAll<Tag_Regiment>()
            .ForEach((Entity regimentEntity, in Flag_Preselection preselectFlag) =>
            {
                bool isPreselected = preselectFlag.IsActive;
                bool isSelected = GetComponent<Flag_Selection>(regimentEntity).IsActive;
                if ((!isPreselected && !isSelected) || (isPreselected && isSelected)) return;
                SetComponent(regimentEntity, new Flag_Selection(){IsActive = isPreselected});
                SetComponent(regimentEntity, new Filter_Selection(){DidChange = true});
            }).Run();
        }
    }
    
}