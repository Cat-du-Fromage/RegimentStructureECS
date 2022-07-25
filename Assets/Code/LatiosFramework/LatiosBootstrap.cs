using System;
using System.Collections.Generic;
using Latios;
using Latios.Authoring;
using Unity.Entities;

namespace KaizerWald
{
    [UnityEngine.Scripting.Preserve]
    public class LatiosConversionBootstrap : ICustomConversionBootstrap
    {
        public bool InitializeConversion(World conversionWorldWithGroupsAndMappingSystems, CustomConversionSettings settings, ref List<Type> filteredSystems)
        {
            GameObjectConversionGroup defaultGroup = conversionWorldWithGroupsAndMappingSystems.GetExistingSystem<GameObjectConversionGroup>();
            BootstrapTools.InjectSystems(filteredSystems, conversionWorldWithGroupsAndMappingSystems, defaultGroup);

            //Latios.Psyshock.Authoring.PsyshockConversionBootstrap.InstallLegacyColliderConversion(conversionWorldWithGroupsAndMappingSystems);
            Latios.Kinemation.Authoring.KinemationConversionBootstrap.InstallKinemationConversion(conversionWorldWithGroupsAndMappingSystems);
            return true;
        }
    }

    [UnityEngine.Scripting.Preserve]
    public class LatiosBootstrap : ICustomBootstrap
    {
        public unsafe bool Initialize(string defaultWorldName)
        {
            //World world = World.DefaultGameObjectInjectionWorld;
            LatiosWorld world = new LatiosWorld(defaultWorldName);
            World.DefaultGameObjectInjectionWorld = world;

            List<Type> systems = new List<Type>(DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default));
            
            BootstrapTools.InjectSystems(systems, world, world.simulationSystemGroup);
            //SimulationSystemGroup simulationSystemGroup = world.GetExistingSystem<SimulationSystemGroup>();
            //BootstrapTools.InjectSystems(systems, world, simulationSystemGroup);
            
            CoreBootstrap.InstallImprovedTransforms(world);
            //Latios.Myri.MyriBootstrap.InstallMyri(world);
            Latios.Kinemation.KinemationBootstrap.InstallKinemation(world);

            world.initializationSystemGroup.SortSystems();
            world.simulationSystemGroup.SortSystems();
            world.presentationSystemGroup.SortSystems();

            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);
            return true;
        }
    }
}