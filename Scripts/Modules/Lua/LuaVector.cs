
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using MoonSharp.Interpreter;

namespace Modules.Lua
{
    public static class LuaVector
    {
        private static bool register = false;

        public static void RegisterAll()
        {
            if (register) { return; }

            RegisterVector2();
            RegisterVector3();

            register = true;
        }

        private static void RegisterVector2()
        {
            // To create Vector2 in lua:
            // position = {1.0, 1.0}

            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vector2), 
                dynVal =>
                {
                    var table = dynVal.Table;

                    return new Vector2()
                    {
                        x = (float)table.Get(1).Number,
                        y = (float)table.Get(2).Number,
                    };
                });

            Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Vector2>(
                (script, vector) => 
                {
                    var x = DynValue.NewNumber(vector.x);
                    var y = DynValue.NewNumber(vector.y);

                    var dynVal = DynValue.NewTable(script, new DynValue[] { x, y });

                    return dynVal;
                });            
        }

        private static void RegisterVector3()
        {
            // To Vector3 in lua:
            // position = {1.0, 1.0, 1.0}
            
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vector3),
                dynVal => 
                {
                    var table = dynVal.Table;

                    return new Vector3()
                    {
                        x = (float)table.Get(1).Number,
                        y = (float)table.Get(2).Number,
                        z = (float)table.Get(3).Number,
                    };
                });

            Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Vector3>(
                (script, vector) =>
                {
                    var x = DynValue.NewNumber(vector.x);
                    var y = DynValue.NewNumber(vector.y);
                    var z = DynValue.NewNumber(vector.z);

                    var dynVal = DynValue.NewTable(script, new DynValue[] { x, y, z });

                    return dynVal;
                });            
        }

        public static Vector2 ToVector2(this Table table)
        {
            if (table == null || table.Length != 2)
            {
                throw new InvalidCastException();
            }

            var xValue = table.Get(1);
            var yValue = table.Get(2);

            if (xValue.Type != DataType.Number || yValue.Type != DataType.Number)
            {
                throw new ArgumentException();
            }

            return new Vector2()
            {
                x = (float)xValue.Number,
                y = (float)yValue.Number,
            };
        }


        public static Vector3 ToVector3(this Table table)
        {
            if (table == null || table.Length != 3)
            {
                throw new InvalidCastException();
            }

            var xValue = table.Get(1);
            var yValue = table.Get(2);
            var zValue = table.Get(3);

            if (xValue.Type != DataType.Number || yValue.Type != DataType.Number || zValue.Type != DataType.Number)
            {
                throw new ArgumentException();
            }

            return new Vector3()
            {
                x = (float)xValue.Number,
                y = (float)yValue.Number,
                z = (float)zValue.Number,
            };
        }
    }
}

#endif