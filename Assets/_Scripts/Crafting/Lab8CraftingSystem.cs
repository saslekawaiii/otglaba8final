using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab8
{
    public class CraftingSystem : MonoBehaviour
    {
        public event Action Changed;

        public IReadOnlyList<CraftRecipe> Recipes => recipes;

        private readonly List<CraftRecipe> recipes = new List<CraftRecipe>();
        private InventorySystem inventory;

        private void Awake()
        {
            inventory = FindObjectOfType<InventorySystem>();
            BuildDefaultRecipes();
        }

        public bool TryCraft(string recipeId, out string message)
        {
            message = "";
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                message = "Рецепт не найден";
                return false;
            }

            if (inventory == null)
            {
                inventory = FindObjectOfType<InventorySystem>();
            }

            if (inventory == null)
            {
                message = "Инвентарь не найден";
                return false;
            }

            CraftRecipe recipe = recipes.Find(r => string.Equals(r.id, recipeId, StringComparison.OrdinalIgnoreCase));
            if (recipe == null)
            {
                message = "Рецепт не найден";
                return false;
            }

            if (!inventory.TrySpendMany(recipe.inputs))
            {
                message = "Недостаточно ресурсов";
                return false;
            }

            if (recipe.outputs != null)
            {
                for (int i = 0; i < recipe.outputs.Length; i++)
                {
                    RecipeIngredient outItem = recipe.outputs[i];
                    if (outItem == null)
                    {
                        continue;
                    }

                    inventory.Add(outItem.itemId, Mathf.Max(1, outItem.amount));
                }
            }

            PlayerStats stats = FindObjectOfType<PlayerStats>();
            if (stats != null)
            {
                stats.AddExperience(20);
            }

            message = "Создано: " + recipe.title;
            Changed?.Invoke();
            return true;
        }

        private void BuildDefaultRecipes()
        {
            if (recipes.Count > 0)
            {
                return;
            }

            recipes.Add(new CraftRecipe
            {
                id = "plank_recipe",
                title = "Доски",
                description = "Переработка дерева в доски",
                inputs = new[]
                {
                    new RecipeIngredient { itemId = "wood", amount = 4 }
                },
                outputs = new[]
                {
                    new RecipeIngredient { itemId = "plank", amount = 2 }
                }
            });

            recipes.Add(new CraftRecipe
            {
                id = "brick_recipe",
                title = "Каменный блок",
                description = "Обработка камня",
                inputs = new[]
                {
                    new RecipeIngredient { itemId = "stone", amount = 4 }
                },
                outputs = new[]
                {
                    new RecipeIngredient { itemId = "brick", amount = 2 }
                }
            });

            recipes.Add(new CraftRecipe
            {
                id = "rune_recipe",
                title = "Боевая руна",
                description = "Материал для усилений",
                inputs = new[]
                {
                    new RecipeIngredient { itemId = "wood", amount = 6 },
                    new RecipeIngredient { itemId = "stone", amount = 6 }
                },
                outputs = new[]
                {
                    new RecipeIngredient { itemId = "rune", amount = 1 }
                }
            });
        }
    }
}
