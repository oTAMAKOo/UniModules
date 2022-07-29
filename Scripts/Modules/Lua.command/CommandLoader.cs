
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using XLua;
using Extensions;

namespace Modules.Lua.Command
{
    public abstract partial class CommandLoader
    {
        //----- params -----

		private class Node
		{
			public string name = null;

			public List<Node> childs = null;
		}

		//----- field -----

		private LuaController luaController = null;

		private Dictionary<Type, ICommand> commands = null;

        //----- property -----

		public IReadOnlyDictionary<Type, ICommand> Commands { get { return commands; } }

		//----- method -----

		public void Setup(LuaController luaController)
		{
			this.luaController = luaController;

			commands = new Dictionary<Type, ICommand>();

			luaController.LuaEnv.AddLoader(GetCommandLuaBytes);

			luaController.Require("_command_");
		}

		public T GetCommand<T>() where T : class, ICommand
		{
			return commands.GetValueOrDefault(typeof(T)) as T;
		}

		private byte[] GetCommandLuaBytes(ref string luaPath)
		{
			if (luaPath != "_command_"){ return null; }

			var commands = RegisterCommand();

			var lua = new StringBuilder();
			
			lua.Append(BuildTableDefileLua(commands));

			lua.Append(BuildFunctionDefileLua(commands));

			return Encoding.UTF8.GetBytes(lua.ToString());
		}

		private ICommand[] RegisterCommand()
		{
			var types = GetCommandTypes();

			var commandInterfaceType = typeof(ICommand);

			var luaCallAttribute = typeof(CSharpCallLuaAttribute);

			foreach (var type in types)
			{
				try
				{
					// CSharpCallLuaが定義されているか.
					if (type.GetCustomAttribute(luaCallAttribute) == null)
					{
						Debug.LogError($"[Attribute Error] {type.FullName}\nRequire { luaCallAttribute.FullName }\n");
					}

					// ICommandを継承しているか.
					if(!type.GetInterfaces().Contains(commandInterfaceType))
					{
						Debug.LogError($"[Interface Error] {type.FullName}\nRequire { commandInterfaceType.FullName }\n");
					}

					// デフォルトコンストラクタがあるか.
					if (type.GetConstructor(Type.EmptyTypes) == null)
					{
						Debug.LogError($"[Constructor Error] {type.FullName}\n Require default constructor.\n");
					}

					var command = Activator.CreateInstance(type) as ICommand;

					if (command == null)
					{
						throw new Exception($"[Class Error] {type.FullName} create failed.\n");
					}

					OnCreateCommand(command);

					// Luaに登録.

					var luaName = GetInstanceLuaName(command);

					luaController.SetValue(luaName, command);

					// 追加.
					commands.Add(type, command);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			return commands.Values.ToArray();
		}

		/// <summary> コマンドのテーブルLua文字列構築 </summary>
		public string BuildTableDefileLua(ICommand[] commands)
		{
			var list = new List<Node>();

			foreach (var command in commands)
			{
				if (string.IsNullOrEmpty(command.LuaName))
				{
					throw new Exception($"Invalid command define : \nClass : { command.GetType().FullName }\nLuaName : { command.LuaName }\n");
				}

				var tableName = GetTableName(command);

				if (string.IsNullOrEmpty(tableName)){ continue; }

				Node node = null;

				var names = tableName.Split('.');

				foreach (var name in names)
				{
					if (node == null)
                    {
						node = list.FirstOrDefault(x => x.name == name);

						if (node == null)
						{
							node = new Node()
							{
								name = name,
								childs = new List<Node>(),
							};

							list.Add(node);
						}
                    }
					else
					{
						var child = node.childs.FirstOrDefault(x => x.name == name);

						if (child == null)
						{
							child = new Node()
							{
								name = name,
								childs = new List<Node>(),
							};

							node.childs.Add(child);
						}

						node = child;
					}
				}
			}

			// ノード情報からテーブル定義作成.

			var builder = new StringBuilder();

			foreach (var item in list)
			{
				var str = BuildDefineString(item, 0);

				if (!string.IsNullOrEmpty(str))
				{
					builder.AppendLine(str).AppendLine();
				}
			}

			return builder.ToString().FixLineEnd();
		}

		/// <summary> ノードの構造文字列構築 </summary>
		private string BuildDefineString(Node node, int indentLevel)
		{
			if (string.IsNullOrEmpty(node.name)){ return null; }

			var builder = new StringBuilder();

			var indentStr = GetIndent(indentLevel);

			var hasChild = node.childs.Any();

			builder.Append(indentStr).Append($"{node.name} = ");

			if (hasChild)
			{
				builder.AppendLine().Append(indentStr);
			}

			builder.Append("{");

			if (hasChild)
			{
				builder.AppendLine();
			}

			for (var i = 0; i < node.childs.Count; i++)
			{
				var child = node.childs[i];

				if (0 < i)
				{
					builder.AppendLine();
				}

				var defineStr = BuildDefineString(child, indentLevel + 1);

				builder.Append(defineStr);

				if (1 < node.childs.Count && i < node.childs.Count - 1)
				{
					builder.Append(",");
				}

				builder.AppendLine();
			}

			if (hasChild)
			{
				builder.Append(indentStr);
			}

			builder.Append("}");

			return builder.ToString();
		}

		/// <summary> 呼び出し用関数定義Lua文字列構築 </summary>
		private string BuildFunctionDefileLua(ICommand[] commands)
		{
			var builder = new StringBuilder();

			for (var i = 0; i < commands.Length; i++)
			{
				var command = commands[i];

				var methods = command.GetType().GetMethods();

				var methodInfo = methods.FirstOrDefault(x => x.Name == command.Callback);
				
				if (methodInfo == null) { continue; }

				var tableName = GetTableName(command);
				var commandName = GetCommandName(command);

				if (0 < i)
				{
					builder.AppendLine();
				}

				if (!string.IsNullOrEmpty(tableName))
				{
					builder.Append($"{tableName}.");
				}

				builder.Append($"{commandName} = function(");

				var argumentStr = string.Empty;

				var parameters = methodInfo.GetParameters();

				for (var j = 0; j < parameters.Length; j++)
				{
					var param = parameters[j];

					if (0 < j)
					{
						argumentStr += ", ";
					}

					// 可変長引数か.
					if(param.GetCustomAttributes(typeof(ParamArrayAttribute), false).Any())
					{
						argumentStr += "...";
					}
					else
					{
						argumentStr += param.Name;
					}
				}

				builder.AppendLine($"{argumentStr})");

				builder.Append("\t");

				// 戻り値.
				var hasResult = methodInfo.ReturnType != typeof(void);

				if (hasResult)
				{
					builder.Append("return ");
				}

				// asyncメソッドか.
				var isAsync = methodInfo.GetCustomAttributes(typeof(AsyncStateMachineAttribute)).Any();

				if(isAsync)
				{
					builder.Append("await(");
				}

				var luaName = GetInstanceLuaName(command);

				builder.Append($"{luaName}:{command.Callback}({argumentStr})");

				if(isAsync)
				{
					builder.Append(")");
				}

				builder.AppendLine();

				builder.AppendLine("end");
			}

			return builder.ToString().FixLineEnd();
		}

		private string GetInstanceLuaName(ICommand command)
		{
			return "COMMAND_" + command.GetType().FullName.Replace('.', '_');
		}

		private string GetTableName(ICommand command)
		{
			var parts = command.LuaName.Split('.').Select(x => x.Trim()).ToArray();

			if (parts.Any(x => string.IsNullOrEmpty(x)))
			{
				throw new Exception($"Invalid command define : \nClass : { command.GetType().FullName }\nLuaName : { command.LuaName }");
			}
			
			if (parts.Length < 2){ return null; }

			parts =  parts.Take(parts.Length - 1).ToArray();

			return string.Join(".", parts);
		}
		
		private string GetCommandName(ICommand command)
		{
			var parts = command.LuaName.Split('.').Select(x => x.Trim()).ToArray();

			return parts.LastOrDefault();
		}

		private string GetIndent(int indentLevel)
		{
			var builder = new StringBuilder();

			for (var i = 0; i < indentLevel; i++)
			{
				builder.Append("\t");
			}

			return builder.ToString();
		}

		protected virtual void OnCreateCommand(ICommand command){ }

		protected abstract IEnumerable<Type> GetCommandTypes();
	}
}