using System;
using System.Collections.Generic;
using Yafc.Model;

namespace Yafc.Parser;

internal partial class FactorioDataDeserializer {
    private void DeserializeFusionGenerator(LuaTable table) {
        var fusionGenerator = GetObject<Entity, EntityCrafter>(table);

        // energy_source :: ElectricEnergySource
        // output_flow_limit is mandatory and must be positive.
        if (table.Get("energy_source", out LuaTable? genEnergySource) && genEnergySource.Get("output_flow_limit", out string? outputFlowLimit)) {
            fusionGenerator.basePower = ParseEnergy(outputFlowLimit);
        }

        // input_fluid_box :: FluidBox
        // filter is mandatory.
        if (GetFluidBoxFilter(table, "input_fluid_box", 0, out Fluid? inputFluid, out TemperatureRange inputFluidTemperature)) {
            // nothing
        }
        else {
            throw new Exception("invalid fusion generator");
        }
        
        // output_fluid_box :: FluidBox
        // filter is mandatory.
        if (GetFluidBoxFilter(table, "output_fluid_box", 0, out Fluid? outputFluid, out TemperatureRange outputFluidTemperature)) {
            // nothing
        }
        else {
            throw new Exception("invalid fusion generator");
        }

        // max_fluid_usage :: FluidAmount
        // Must be positive.
        if (table.Get("max_fluid_usage", out float maxFluidUsagePerTick)) {
            // nothing
        }
        else {
            throw new Exception("invalid fusion generator");
        }


        string fuelCategory = SpecialNames.SpecificFluid + inputFluid.name;
        fuelUsers.Add(fusionGenerator, fuelCategory);
            
        inputFluid.SetTemperature(inputFluidTemperature.min);
        fusionGenerator.energy = new EntityEnergy {
            type = EntityEnergyType.FluidHeat,
            effectivity = 1f,
            acceptedTemperature = inputFluidTemperature,
            // it's effectively burning the temperature, so the min is set to 0 to emulate that
            workingTemperature = new TemperatureRange(0, inputFluid.temperatureRange.max),
        };
        
        var production = CreateSpecialRecipe(electricity, SpecialNames.GeneratorRecipe, "generating.fusion");
        production.ingredients = [];
        production.products = [new Product(electricity, 1f), new Product(outputFluid, maxFluidUsagePerTick * 60 / fusionGenerator.basePower)];
        production.flags |= RecipeFlags.ScaleProductionWithPower;
        
        recipeCrafters.Add(fusionGenerator, SpecialNames.GeneratorRecipe);
    }
    
    private void DeserializeFusionReactor(LuaTable table)
    {
        var fusionReactor = GetObject<Entity, EntityFusionReactor>(table);
        
        // energy_source :: ElectricEnergySource
        // First energy source for the process: provides energy
        if (table.Get("energy_source", out LuaTable? _)) {
            // TODO this should be captured & handled.
        }
        else {
            throw new Exception("invalid fusion reactor");
        }
        
        // power_input :: Energy
        // Power input consumed from first energy source at full performance.
        if (table.Get("power_input", out string? powerInput)) {
            // do nothing
        }
        else {
            throw new Exception("invalid fusion reactor");
        }
        
        // burner :: BurnerEnergySource
        // Second energy source for the process: provides fuel
        if (table.Get("burner", out LuaTable? burner)) {
            ReadEnergySource(burner, fusionReactor);
        }
        else {
            throw new Exception("invalid fusion reactor");
        }
        
        // input_fluid_box :: FluidBox
        // The input fluid box. filter is mandatory.
        if (GetFluidBoxFilter(table, "input_fluid_box", 0, out Fluid? inputFluid, out TemperatureRange inputFluidTemperature)) {
            fusionReactor.fluidInputs = 1;
        }
        else {
            throw new Exception("invalid fusion reactor");
        }
                
        // max_fluid_usage :: FluidAmount
        // Maximum amount of fluid converted from input_fluid_box to output_fluid_box within a single tick. Must be positive.
        float maxFluidUsage;
        if (table.Get("max_fluid_usage", out float maxFluidUsagePerTick)) {
            maxFluidUsage = maxFluidUsagePerTick * 60f;
        }
        else {
            throw new Exception("invalid fusion reactor");
        }
        
        // output_fluid_box :: FluidBox
        // The output fluid box. filter is mandatory.
        if (GetFluidBoxFilter(table, "output_fluid_box", 0, out Fluid? outputFluid, out TemperatureRange outputFluidTemperature)) {
            fusionReactor.basePower = outputFluidTemperature.min * outputFluid.heatCapacity * maxFluidUsage;
        }
        else {
            throw new Exception("invalid fusion reactor");
        }
        
        // neighbour_bonus :: float optional
        // Default: 1
        fusionReactor.fusionReactorNeighborBonus = table.Get("neighbour_bonus", 1f);
        
        string fusionCategory = SpecialNames.BoilerRecipe + fusionReactor.name;
        var fusionRecipe = CreateSpecialRecipe(outputFluid, fusionCategory, "processing in fusion reactor");
        recipeCrafters.Add(fusionReactor, fusionCategory);
        
        fusionRecipe.ingredients = [
            // electricity needs to be an ingredient since the burner fuel is variable and needs to be the fuel source
            new Ingredient(electricity, ParseEnergy(powerInput)), 
            new Ingredient(inputFluid, maxFluidUsage)
        ];
        fusionRecipe.products = [new Product(outputFluid, maxFluidUsage)];
        fusionRecipe.time = 1f;
        fusionReactor.baseCraftingSpeed = 1f;
    }
}
