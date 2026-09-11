using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CompiledAvaloniaXaml;
using LlamaStudio.Converters;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Core.Services;
using LlamaStudio.Infrastructure.Chat;
using LlamaStudio.Infrastructure.HuggingFace;
using LlamaStudio.Infrastructure.Llama;
using LlamaStudio.Infrastructure.Logging;
using LlamaStudio.Infrastructure.Mcp;
using LlamaStudio.Infrastructure.Models;
using LlamaStudio.Infrastructure.Profiles;
using LlamaStudio.Infrastructure.Updater;
using LlamaStudio.Services;
using LlamaStudio.ViewModels;
using LlamaStudio.Views;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;

namespace LlamaStudio;

[CompilerGenerated]
public class App : Application
{
	[CompilerGenerated]
	private class XamlClosure_1
	{
		public static object? Build_1(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294309365u)
			};
		}

		public static CompiledAvaloniaXaml.XamlIlContext.Context<App?>? CreateContext(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = new CompiledAvaloniaXaml.XamlIlContext.Context<App>(P_0, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FApp_002Eaxaml.Singleton }, "avares://LlamaStudio/App.axaml");
			if (P_0 != null)
			{
				object service = P_0.GetService(typeof(IRootObjectProvider));
				if (service != null)
				{
					service = ((IRootObjectProvider)service).RootObject;
					context.RootObject = (App)service;
				}
			}
			return context;
		}

		public static object? Build_2(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294638330u)
			};
		}

		public static object? Build_3(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(uint.MaxValue)
			};
		}

		public static object? Build_4(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4280230696u)
			};
		}

		public static object? Build_5(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4284837238u)
			};
		}

		public static object? Build_6(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4287337630u)
			};
		}

		public static object? Build_7(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4291876830u)
			};
		}

		public static object? Build_8(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4291416537u)
			};
		}

		public static object? Build_9(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294375674u)
			};
		}

		public static object? Build_10(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4281303543u)
			};
		}

		public static object? Build_11(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4279926730u)
			};
		}

		public static object? Build_12(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4280163147u)
			};
		}

		public static object? Build_13(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4278603323u)
			};
		}

		public static object? Build_14(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4286328082u)
			};
		}

		public static object? Build_15(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4280171103u)
			};
		}

		public static object? Build_16(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4279706964u)
			};
		}

		public static object? Build_17(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4280227123u)
			};
		}

		public static object? Build_18(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4284704497u)
			};
		}

		public static object? Build_19(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4279286145u)
			};
		}

		public static object? Build_20(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294286859u)
			};
		}

		public static object? Build_21(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4282090230u)
			};
		}

		public static object? Build_22(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4287323382u)
			};
		}

		public static object? Build_23(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4286680312u)
			};
		}

		public static object? Build_24(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4281652121u)
			};
		}

		public static object? Build_25(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294818420u)
			};
		}

		public static object? Build_26(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4284524026u)
			};
		}

		public static object? Build_27(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4284524026u)
			};
		}

		public static object? Build_28(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4289049852u)
			};
		}

		public static object? Build_29(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4280227123u)
			};
		}

		public static object? Build_30(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4281146217u)
			};
		}

		public static object? Build_31(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4289170426u)
			};
		}

		public static object? Build_32(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4281146217u)
			};
		}

		public static object? Build_33(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4282090230u)
			};
		}

		public static object? Build_34(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4279286145u)
			};
		}

		public static object? Build_35(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294286859u)
			};
		}

		public static object? Build_36(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4287323382u)
			};
		}

		public static object? Build_37(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4279286145u)
			};
		}

		public static object? Build_38(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4293980400u)
			};
		}

		public static object? Build_39(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4293454056u)
			};
		}

		public static object? Build_40(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4292138196u)
			};
		}

		public static object? Build_41(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4283916394u)
			};
		}

		public static object? Build_42(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4280230696u)
			};
		}

		public static object? Build_43(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4293454056u)
			};
		}

		public static object? Build_44(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4283916394u)
			};
		}

		public static object? Build_45(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4281303543u)
			};
		}

		public static object? Build_46(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(uint.MaxValue)
			};
		}

		public static object? Build_47(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4292138196u)
			};
		}

		public static object? Build_48(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294375674u)
			};
		}

		public static object? Build_49(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294375674u)
			};
		}

		public static object? Build_50(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4286331629u)
			};
		}

		public static object? Build_51(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4278556265u)
			};
		}

		public static object? Build_52(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4280640491u)
			};
		}

		public static object? Build_53(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4292441862u)
			};
		}

		public static object? Build_54(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4278751666u)
			};
		}

		public static object? Build_55(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(uint.MaxValue)
			};
		}

		public static object? Build_56(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4280163147u)
			};
		}

		public static object? Build_57(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4286680312u)
			};
		}

		public static object? Build_58(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4284773515u)
			};
		}

		public static object? Build_59(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(uint.MaxValue)
			};
		}

		public static object? Build_60(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4293060848u)
			};
		}

		public static object? Build_61(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4284773515u)
			};
		}

		public static object? Build_62(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4282595530u)
			};
		}

		public static object? Build_63(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4278556265u)
			};
		}

		public static object? Build_64(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4292617766u)
			};
		}

		public static object? Build_65(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4282865001u)
			};
		}

		public static object? Build_66(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4286331629u)
			};
		}

		public static object? Build_67(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4278751666u)
			};
		}

		public static object? Build_68(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new EqualConverter();
		}

		public static object? Build_69(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new BoolToStringConverter();
		}

		public static object? Build_70(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new GreaterThanConverter();
		}

		public static object? Build_71(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new NotNullConverter();
		}

		public static object? Build_72(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new InvertedBoolConverter();
		}

		public static object? Build_73(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new IntToBoolConverter();
		}

		public static object? Build_74(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new NotEqualConverter();
		}

		public static object? Build_75(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new StringEqualBrushConverter();
		}

		public static object? Build_76(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SizeFormatConverter();
		}

		public static object? Build_77(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new LogLevelBrushConverter();
		}

		public static object? Build_78(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new BuildTypeToBrushConverter();
		}

		public static object? Build_79(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new PrereleaseBorderConverter();
		}

		public static object? Build_80(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ContainsConverter();
		}

		public static object? Build_81(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new NotContainsConverter();
		}

		public static object? Build_82(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ChatRoleToBrushConverter();
		}

		public static object? Build_83(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ChatRoleToInitialConverter();
		}

		public static object? Build_84(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ChatRoleToNameConverter();
		}

		public static object? Build_85(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ChatRoleTextBrushConverter();
		}

		public static object? Build_86(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new NullVisibilityConverter();
		}

		public static object? Build_87(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ServerConnectionStatusConverter();
		}

		public static object? Build_88(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ServerStateBrushConverter();
		}

		public static object? Build_89(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new HasItemsVisibilityConverter();
		}

		public static object? Build_90(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new Base64ToImageConverter();
		}

		public static object? Build_91(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new McpTransportToVisibilityConverter();
		}

		public static object? Build_92(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new McpConnectedToBrushConverter();
		}

		public static object? Build_93(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new BookmarkBrushConverter();
		}

		public static object? Build_94(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new BookmarkTooltipConverter();
		}

		public static object? Build_95(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ChatRoleToVisibilityConverter();
		}

		public static object? Build_96(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ReasoningVisibilityConverter();
		}

		public static object? Build_97(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ToolResultLengthConverter();
		}

		public static object? Build_98(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ReasoningChevronConverter();
		}

		public static object? Build_99(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new McpChipBgConverter();
		}

		public static object? Build_100(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new McpChipFgConverter();
		}

		public static object? Build_101(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new McpChipOpacityConverter();
		}

		public static object? Build_102(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new McpToggleBgConverter();
		}

		public static object? Build_103(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new McpToggleFgConverter();
		}

		public static object? Build_104(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new ContextProgressBarColorConverter();
		}

		public static object? Build_105(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SessionSelectedBgConverter();
		}

		public static object? Build_106(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new BoolToIndexConverter();
		}

		public static object? Build_107(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new EqualsZeroConverter();
		}

		public static object? Build_108(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new IntTextConverter();
		}

		public static object? Build_109(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new FloatTextConverter();
		}

		public static object? Build_110(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new StringTextConverter();
		}

		public static object? Build_111(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new DoubleTextConverter();
		}

		public static object? Build_112(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new DefaultToOpacityConverter();
		}

		public static object? Build_113(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4284524026u)
			};
		}

		public static object? Build_114(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4281652121u)
			};
		}

		public static object? Build_115(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294688548u)
			};
		}

		public static object? Build_116(IServiceProvider? P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<App> context = CreateContext(P_0);
			return new SolidColorBrush
			{
				Color = Color.FromUInt32(4294472049u)
			};
		}
	}

	private static IHost? _host;

	private TrayManager? _trayManager;

	private TrayIcon? _linuxTrayIcon;


	private static Action<object?>? _0021XamlIlPopulateOverride;

	public static IServiceProvider? Services { get; private set; }

	public static bool IsShuttingDown { get; private set; }

	public static bool IsTrayAvailable { get; private set; }

	public override void Initialize()
	{
		base.Initialize();
	}

	public override async void OnFrameworkInitializationCompleted()
	{
		try
		{
			Services = (_host = CreateHost()).Services;
			ISettings settings = Services.GetRequiredService<ISettings>();
			await settings.LoadAsync();
			// Keep the Linux desktop entry tied to the binary that is actually
			// running. This also repairs stale autostart entries after switching
			// between development builds without requiring the Settings page.
			SettingsViewModel.ApplyAutoStart(settings.AutoStartWithOS);
			try
			{
				await Services.GetRequiredService<WebUiService>().StartAsync();
			}
			catch (Exception webException)
			{
				System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LlamaStudio-recovered.log"), $"{DateTime.Now:O} [WEBUI] {webException}\n");
			}
			IVersionCache versionCache = Services.GetRequiredService<IVersionCache>();
			await versionCache.LoadAsync();
			// r96: do NOT block the UI thread (and the first window) on the
			// llama.cpp scan: on Windows at boot every "llama-server.exe
			// --version" probe can be slowed by the antivirus, delaying the
			// window by a minute or more (the app looked like a silent zombie).
			Task.Run(async delegate
			{
				try
				{
					await versionCache.ScanAsync(settings.LlamaCppBaseDirectory);
				}
				catch (Exception scanException)
				{
					System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LlamaStudio-recovered.log"), $"{DateTime.Now:O} [SCAN] {scanException}\n");
				}
			});
			ApplyTheme(settings.Theme);
			ILocalizationService requiredService = Services.GetRequiredService<ILocalizationService>();
			if (!string.IsNullOrEmpty(settings.Language))
			{
				requiredService.ChangeLanguage(settings.Language);
			}
		}
		catch (Exception ex)
		{
			System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LlamaStudio-recovered.log"), $"{DateTime.Now:O} [INIT] {ex}\n");
			// r96: a failed init must not leave a windowless, trayless zombie —
			// show the reason and exit (the user relaunches manually).
			ShowStartupFailure("[INIT] инициализация", ex);
			base.OnFrameworkInitializationCompleted();
			(base.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
			return;
		}
		if (base.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
		{
			bool mainWindowIsVisible = false;
			try
			{
				MainWindow mainWindow = (MainWindow)(classicDesktopStyleApplicationLifetime.MainWindow = Services.GetRequiredService<MainWindow>());
				if (OperatingSystem.IsWindows())
				{
					_trayManager = Services.GetRequiredService<TrayManager>();
					_trayManager.Initialize();
					// r86: port of the Windows r25/r26 tray fix — propagate the tray
					// initialization state. Without it MainWindow.OnClosing treated
					// the tray as unavailable and exited the process even when the
					// user had enabled "Minimize to tray" (StartMinimized was also
					// effectively ignored on Windows).
					IsTrayAvailable = _trayManager.IsAvailable;
					if (!IsTrayAvailable)
					{
						Services.GetRequiredService<ILogService>().Warning("System tray is unavailable; closing the window will exit the application.", "Tray");
					}
					if (!IsTrayAvailable)
					{
						// r96: at Windows boot the notification area can be absent
						// when the autostart task runs — retry adding the icon
						// instead of leaving the app without a tray forever.
						_ = Task.Run(async delegate
						{
							for (int trayAttempt = 1; trayAttempt <= 15 && !IsTrayAvailable; trayAttempt++)
							{
								await Task.Delay(2000);
								if (_trayManager.TryReadd())
								{
									Dispatcher.UIThread.Post(delegate
									{
										IsTrayAvailable = true;
									});
									System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LlamaStudio-recovered.log"), $"{DateTime.Now:O} [TRAY] icon added after retry {trayAttempt}\n");
									break;
								}
							}
						});
					}
				}
				else if (OperatingSystem.IsLinux())
				{
					InitializeLinuxTray();
				}
				ISettings requiredService3 = Services.GetRequiredService<ISettings>();
				// r98: the "Показывать окно программы" (ShowDesktopWindow) option is
				// removed — the window is always available at startup (the old flag
				// let the app run windowless and produced the "process without
				// window/tray" state). "Запускать свёрнутым в трей" (StartMinimized)
				// still starts the app in the tray; if the tray is not ready yet
				// (Windows boot) the window is shown instead of disappearing.
				if (!requiredService3.StartMinimized || !IsTrayAvailable)
				{
					mainWindow.Show();
					mainWindowIsVisible = true;
				}
				if (requiredService3.StartMonitoringWindow)
				{
					ShowMonitoringWindow();
				}
				Dispatcher.UIThread.Post(() => NormalizeThemeVisuals(this, RequestedThemeVariant == ThemeVariant.Light));
				if (requiredService3.AutoStartServer)
				{
					Task.Run(async delegate
					{
						await Task.Delay(2000);
						try
						{
							IProfileManager profileManager = Services?.GetService<IProfileManager>();
							IServerManager serverManager = Services?.GetService<IServerManager>();
							if (profileManager != null && serverManager != null)
							{
								ServerProfile? serverProfile = null;
								if (!string.IsNullOrWhiteSpace(requiredService3.LastSelectedProfileId))
								{
									serverProfile = await profileManager.GetProfileAsync(requiredService3.LastSelectedProfileId);
								}
								serverProfile ??= await profileManager.GetDefaultProfileAsync();
								if (serverProfile != null && !string.IsNullOrWhiteSpace(serverProfile.ModelPath))
								{
									await serverManager.StartAsync(serverProfile);
								}
							}
						}
						catch
						{
						}
					});
				}
			}
			catch (Exception ex)
			{
				System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LlamaStudio-recovered.log"), $"{DateTime.Now:O} [WINDOW] {ex}\n");
				// r96: no silent zombies — if neither the window nor the tray
				// is up, the user has nothing to click: show the error and exit.
				if (mainWindowIsVisible == false && !IsTrayAvailable)
				{
					ShowStartupFailure("[WINDOW] окно и трей", ex);
					classicDesktopStyleApplicationLifetime.Shutdown();
				}
			}
			classicDesktopStyleApplicationLifetime.Exit += delegate
			{
				IsShuttingDown = true;
				try
				{
					// Stop the network stream while preserving .part files for the next launch.
					Services.GetService<IHuggingFaceDownloader>()?.PauseDownloads();
				}
				catch
				{
				}
				try
				{
					_trayManager?.Dispose();
					_linuxTrayIcon?.Dispose();
					IsTrayAvailable = false;
				}
				catch
				{
				}
				try
			{
					Services.GetService<SettingsViewModel>()?.ApplySettingsToModel();
					ISettings? service = Services.GetService<ISettings>();
					service.Save();
					if (!service.KeepServerOnExit)
					{
						IServerManager? service2 = Services.GetService<IServerManager>();
						if (service2 != null)
						{
							Task.Run(async delegate
							{
								await service2.StopAsync();
							}).GetAwaiter().GetResult();
						}
					}
				}
				catch
				{
				}
				try
				{
					Services.GetService<WebUiService>()?.StopAsync().GetAwaiter().GetResult();
				}
				catch
				{
				}
				try
				{
					if (_host != null)
					{
						Task.Run(async delegate
						{
							await _host.StopAsync();
						}).GetAwaiter().GetResult();
					}
				}
				catch
				{
				}
			};
		}
		base.OnFrameworkInitializationCompleted();
	}

	private static void ShowWindowFromTray()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
		{
			classicDesktopStyleApplicationLifetime.MainWindow?.Show();
			classicDesktopStyleApplicationLifetime.MainWindow?.Activate();
			Window? mainWindow = classicDesktopStyleApplicationLifetime.MainWindow;
			if (mainWindow != null && mainWindow.WindowState == WindowState.Minimized)
			{
				classicDesktopStyleApplicationLifetime.MainWindow.WindowState = WindowState.Normal;
			}
		}
	}

	private static void ShowMonitoringWindow()
	{
		try
		{
			if (Services?.GetService(typeof(MonitoringViewModel)) is MonitoringViewModel vm)
			{
				new MonitoringWindow(vm).Show();
			}
		}
		catch
		{
		}
	}

	private static void RequestExitFromTray(bool force)
	{
		IsShuttingDown = true;
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
		{
			classicDesktopStyleApplicationLifetime.Shutdown();
		}
	}

	public void ShowWindow()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
		{
			classicDesktopStyleApplicationLifetime.MainWindow?.Show();
			classicDesktopStyleApplicationLifetime.MainWindow?.Activate();
			Window? mainWindow = classicDesktopStyleApplicationLifetime.MainWindow;
			if (mainWindow != null && mainWindow.WindowState == WindowState.Minimized)
			{
				classicDesktopStyleApplicationLifetime.MainWindow.WindowState = WindowState.Normal;
			}
		}
	}

	public void HideWindow()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
		{
			classicDesktopStyleApplicationLifetime.MainWindow?.Hide();
		}
	}

	public void RequestExit(bool force)
	{
		IsShuttingDown = true;
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
		{
			classicDesktopStyleApplicationLifetime.Shutdown();
		}
	}

	private static IHost CreateHost()
	{
		return Host.CreateDefaultBuilder().ConfigureServices(delegate(HostBuilderContext context, IServiceCollection services)
		{
			HttpClient httpClient = new HttpClient(new SocketsHttpHandler
			{
				SslOptions = new SslClientAuthenticationOptions
				{
					EnabledSslProtocols = (SslProtocols.Tls12 | SslProtocols.Tls13)
				},
				PooledConnectionLifetime = TimeSpan.FromMinutes(5.0),
				MaxConnectionsPerServer = 4
			});
			httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LlamaStudio/1.0");
			// r65b: timeout задаётся ОДИН раз при регистрации. Сеттеры .Timeout в сервисах
			// (AppUpdater/LlamaUpdater/HuggingFaceDownloader) удалены: после первого запроса
			// общего клиента .Timeout менять нельзя (InvalidOperationException), а r65 стал
			// резолвить LlamaReleasesViewModel на старте (web-инсталлятор), и его
			// CheckForUpdates() отправлял запрос ДО создания окна -> AppUpdater падал,
			// окно не создавалось.
			httpClient.Timeout = TimeSpan.FromMinutes(30.0);
			services.AddSingleton(httpClient);
			services.AddSingleton<ILocalizationService, LocalizationService>();
			services.AddSingleton<ISettings, AppSettings>();
			services.AddSingleton<ILogService, LogService>();
			services.AddSingleton((Func<IServiceProvider, ILlamaUpdater>)((IServiceProvider sp) => new LlamaUpdater(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<ILogService>(), sp.GetRequiredService<ISettings>(), sp.GetRequiredService<ICliValidator>(), sp.GetRequiredService<IHelpParser>(), sp.GetRequiredService<IVersionCache>())));
			services.AddSingleton((Func<IServiceProvider, IAppUpdater>)((IServiceProvider sp) => new AppUpdater(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<ILogService>())));
			services.AddSingleton<IVersionDetector, VersionDetector>();
			services.AddSingleton<IVersionCache, VersionCache>();
			services.AddSingleton<IServerManager, ServerManager>();
			services.AddSingleton<WebUiService>();
			services.AddSingleton<IModelScanner, ModelScanner>();
			services.AddSingleton<IProfileManager, ProfileManager>();
			services.AddSingleton<IHelpParser, HelpParser>();
			services.AddSingleton<ICliValidator, CliValidator>();
			services.AddSingleton((Func<IServiceProvider, IHuggingFaceDownloader>)((IServiceProvider sp) => new HuggingFaceDownloader(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<ILogService>(), sp.GetRequiredService<ILocalizationService>())));
			services.AddSingleton<IGpuMonitor, GpuMonitorService>();
			services.AddSingleton<IChatService, ChatService>();
			services.AddSingleton<IMcpToolsService, McpToolsService>((IServiceProvider sp) => new McpToolsService(sp.GetRequiredService<ILogService>(), sp.GetRequiredService<ISettings>(), sp.GetService<ILocalizationService>()));
			services.AddSingleton<ChatSessionStore>();
			services.AddSingleton((Func<IServiceProvider, INavigationService>)((IServiceProvider sp) => new NavigationService(() => sp.GetRequiredService<MainViewModel>())));
			services.AddSingleton<MainViewModel>();
			services.AddSingleton<DashboardViewModel>();
			services.AddSingleton<ModelsViewModel>();
			services.AddSingleton<MonitoringViewModel>();
			services.AddSingleton<ServerViewModel>();
			services.AddSingleton<ProfilesViewModel>();
			services.AddSingleton<LogsViewModel>();
			services.AddSingleton<SettingsViewModel>();
			services.AddSingleton<ApiTestViewModel>();
			services.AddSingleton<ChatViewModel>();
			services.AddSingleton<LlamaReleasesViewModel>();
			services.AddSingleton<SupportViewModel>();
			services.AddSingleton<MainWindow>();
			services.AddSingleton((Func<IServiceProvider, IDialogService>)((IServiceProvider sp) => new DialogService(() => sp.GetRequiredService<MainWindow>(), sp.GetRequiredService<ILogService>(), sp.GetService<ILocalizationService>())));
			services.AddSingleton((Func<IServiceProvider, IFilePickerService>)((IServiceProvider sp) => new FilePickerService(() => sp.GetRequiredService<MainWindow>())));
			services.AddSingleton((IServiceProvider sp) => new TrayManager(sp.GetRequiredService<ISettings>(), sp.GetRequiredService<IServerManager>(), sp.GetRequiredService<IProfileManager>(), sp.GetRequiredService<ILocalizationService>(), sp.GetRequiredService<ILogService>(), delegate
			{
				ShowWindowFromTray();
			}, delegate(bool force)
			{
				RequestExitFromTray(force);
			}));
		}).Build();
	}

	internal static void ApplyTheme(string? theme)
	{
		// Light and System themes are intentionally disabled until the visual layer is fully migrated.
		const string text = "Dark";
		ThemeVariant themeVariant = ThemeVariant.Dark;
		ThemeVariant themeVariant2 = themeVariant;
		if (Application.Current is App app)
		{
			app.RequestedThemeVariant = themeVariant2;
			bool lightMode = themeVariant2 == ThemeVariant.Light;
			ApplyThemeColors(app, lightMode);
			Dispatcher.UIThread.Post(() => NormalizeThemeVisuals(app, lightMode));
		}
	}

	private static async Task StartServerFromLinuxTrayAsync()
	{
		IProfileManager? profiles = Services?.GetService<IProfileManager>();
		IServerManager? server = Services?.GetService<IServerManager>();
		ISettings? settings = Services?.GetService<ISettings>();
		if (profiles == null || server == null || settings == null)
		{
			return;
		}
		ServerProfile? profile = !string.IsNullOrWhiteSpace(settings.LastSelectedProfileId)
			? await profiles.GetProfileAsync(settings.LastSelectedProfileId)
			: null;
		profile ??= await profiles.GetDefaultProfileAsync();
		if (profile != null && !string.IsNullOrWhiteSpace(profile.ModelPath))
		{
			await server.StartAsync(profile);
		}
	}

	private static async Task RestartServerFromLinuxTrayAsync()
	{
		IServerManager? server = Services?.GetService<IServerManager>();
		if (server == null)
		{
			return;
		}
		await server.StopAsync();
		await StartServerFromLinuxTrayAsync();
	}

	private static async Task SelectLinuxTrayProfileAsync(string profileId)
	{
		IProfileManager? profiles = Services?.GetService<IProfileManager>();
		ISettings? settings = Services?.GetService<ISettings>();
		if (profiles == null || settings == null)
		{
			return;
		}
		settings.LastSelectedProfileId = profileId;
		settings.Save();
		await profiles.NotifyProfileChanged(profileId);
	}

	private void InitializeLinuxTray()
	{
		try
		{
			NativeMenu menu = new NativeMenu();
			NativeMenuItem showItem = new NativeMenuItem { Header = "Открыть Llama Studio" };
			showItem.Click += delegate { ShowWindowFromTray(); };
			NativeMenu profilesMenu = new NativeMenu();
			ISettings? settings = Services?.GetService<ISettings>();
			IProfileManager? profiles = Services?.GetService<IProfileManager>();
			if (profiles != null)
			{
				foreach (ServerProfile profile in profiles.GetAllProfiles())
				{
					string profileId = profile.Id;
					NativeMenuItem profileItem = new NativeMenuItem
					{
						Header = profileId == settings?.LastSelectedProfileId ? "✓ " + profile.Name : profile.Name
					};
					profileItem.Click += async delegate { await SelectLinuxTrayProfileAsync(profileId); };
					profilesMenu.Add(profileItem);
				}
			}
			menu.Add(new NativeMenuItem { Header = "Профиль", Menu = profilesMenu });
			NativeMenuItem startItem = new NativeMenuItem { Header = "Запустить сервер" };
			startItem.Click += async delegate { await StartServerFromLinuxTrayAsync(); };
			NativeMenuItem stopItem = new NativeMenuItem { Header = "Остановить сервер" };
			stopItem.Click += async delegate
			{
				IServerManager? server = Services?.GetService<IServerManager>();
				if (server != null)
				{
					await server.StopAsync();
				}
			};
			NativeMenuItem restartItem = new NativeMenuItem { Header = "Перезапустить сервер" };
			restartItem.Click += async delegate { await RestartServerFromLinuxTrayAsync(); };
			NativeMenuItem monitoringItem = new NativeMenuItem { Header = "Окно мониторинга" };
			monitoringItem.Click += delegate { ShowMonitoringWindow(); };
			NativeMenuItem exitItem = new NativeMenuItem { Header = "Выход" };
			exitItem.Click += delegate { RequestExitFromTray(force: true); };
			menu.Add(showItem);
			menu.Add(new NativeMenuItemSeparator());
			menu.Add(startItem);
			menu.Add(stopItem);
			menu.Add(restartItem);
			menu.Add(monitoringItem);
			menu.Add(new NativeMenuItemSeparator());
			menu.Add(exitItem);

			_linuxTrayIcon = new TrayIcon
			{
				ToolTipText = "LlamaStudio",
				Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://LlamaStudio/Assets/app-icon.png"))),
				Menu = menu,
				IsVisible = true
			};
			_linuxTrayIcon.Clicked += delegate { ShowWindowFromTray(); };
			TrayIcon.SetIcons(this, new TrayIcons { _linuxTrayIcon });
			IsTrayAvailable = true;
		}
		catch (Exception ex)
		{
			IsTrayAvailable = false;
			System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LlamaStudio-recovered.log"), $"{DateTime.Now:O} [TRAY] {ex}\n");
		}
	}

	private static void NormalizeThemeVisuals(App app, bool lightMode)
	{
		if (app.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
		{
			return;
		}

		foreach (Window window in desktop.Windows)
		{
			NormalizeThemeVisual(window, lightMode, navigation: false);
		}
	}

	private static void NormalizeThemeVisual(Visual visual, bool lightMode, bool navigation)
	{
		if (visual is Button button && IsNavigationButton(button))
		{
			navigation = true;
			button.Foreground = new SolidColorBrush(Color.Parse(lightMode ? "#1F2328" : "#FFFFFF"));
		}
		if (navigation && visual is TextBlock textBlock)
		{
			textBlock.Foreground = new SolidColorBrush(Color.Parse(lightMode ? "#1F2328" : "#FFFFFF"));
		}
		else if (navigation && visual is MaterialIcon icon)
		{
			icon.Foreground = new SolidColorBrush(Color.Parse(lightMode ? "#1F2328" : "#FFFFFF"));
		}

		foreach (Visual child in visual.GetVisualChildren())
		{
			NormalizeThemeVisual(child, lightMode, navigation);
		}
	}

	private static bool IsNavigationButton(Button button)
	{
		return button.CommandParameter is string page && page switch
		{
			"Dashboard" or "Chat" or "Models" or "Monitoring" or "Releases" or "Server" or "Logs" or "Settings" or "Support" => true,
			_ => false
		};
	}

	private static IBrush? MapThemeBrush(IBrush? brush, bool lightMode, bool background, bool text = false)
	{
		if (brush is not SolidColorBrush solid)
		{
			return brush;
		}

		string hex = solid.Color.ToString();
		string? mapped = null;
		if (background)
		{
			mapped = lightMode ? hex switch
			{
				"#0F172AFF" => "#F5F5F5",
				"#1E293BFF" => "#FAFAFA",
				"#1F1533FF" => "#FFFFFF",
				"#2D1B69FF" => "#F6F8FA",
				"#111A2AFF" => "#F6F8FA",
				_ => null
			} : hex switch
			{
				"#F5F5F5FF" => "#0F172A",
				"#FAFAFAFF" => "#1E293B",
				"#FFFFFFFF" => "#1F1533",
				"#F6F8FAFF" => "#2D1B69",
				_ => null
			};
		}
		else if (text)
		{
			mapped = lightMode ? hex switch
			{
				"#FFFFFFFF" or "#F4F7FBFF" => "#1F2328",
				"#94A3B8FF" or "#A8AFBFFF" => "#656D76",
				"#64748BFF" => "#8B949E",
				_ => null
			} : hex switch
			{
				"#1F2328FF" => "#FFFFFFFF",
				"#656D76FF" => "#94A3B8",
				"#8B949EFF" => "#64748B",
				_ => null
			};
		}
		else
		{
			mapped = lightMode ? hex switch
			{
				"#334155FF" or "#3A4B68FF" => "#D0D7DE",
				"#475569FF" => "#C9D1D9",
				_ => null
			} : hex switch
			{
				"#D0D7DEFF" or "#C9D1D9FF" => "#334155",
				_ => null
			};
		}

		return mapped == null ? brush : new SolidColorBrush(Color.Parse(mapped));
	}

	private static ThemeVariant DetectSystemThemeVariant()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			object obj = registryKey?.GetValue("AppsUseLightTheme");
			int result;
			if (obj != null)
			{
				return (int.TryParse(obj.ToString(), out result) && result == 1) ? ThemeVariant.Light : ThemeVariant.Dark;
			}
		}
		catch
		{
		}
		return ThemeVariant.Light;
	}

	private static void ApplyThemeColors(App app, bool lightMode)
	{
		if (lightMode)
		{
			app.Resources["ThemeBackground"] = new SolidColorBrush(Color.Parse("#F7F7F8"));
			app.Resources["ThemeSurface"] = new SolidColorBrush(Color.Parse("#F0F1F3"));
			app.Resources["ThemeCard"] = new SolidColorBrush(Color.Parse("#FBFBFC"));
			app.Resources["ThemeTextPrimary"] = new SolidColorBrush(Color.Parse("#34363B"));
			app.Resources["ThemeTextSecondary"] = new SolidColorBrush(Color.Parse("#5F636B"));
			app.Resources["ThemeTextTertiary"] = new SolidColorBrush(Color.Parse("#7B8088"));
			app.Resources["ThemeBorder"] = new SolidColorBrush(Color.Parse("#D4D7DC"));
			app.Resources["ThemeBorderStrong"] = new SolidColorBrush(Color.Parse("#C4C8CF"));
			app.Resources["ThemeInputBackground"] = new SolidColorBrush(Color.Parse("#F3F4F6"));
			app.Resources["ThemeAccent"] = new SolidColorBrush(Color.Parse("#2F81F7"));
			app.Resources["ThemeAccentHover"] = new SolidColorBrush(Color.Parse("#1A7FCA"));
			app.Resources["CardServerBackground"] = new SolidColorBrush(Color.Parse("#FBFBFC"));
			app.Resources["CardModelsBackground"] = new SolidColorBrush(Color.Parse("#FBFBFC"));
			app.Resources["CardProfilesBackground"] = new SolidColorBrush(Color.Parse("#FBFBFC"));
			app.Resources["CardVersionBackground"] = new SolidColorBrush(Color.Parse("#FBFBFC"));
			app.Resources["CardModelBackground"] = new SolidColorBrush(Color.Parse("#FBFBFC"));
			app.Resources["CardSectionBackground"] = new SolidColorBrush(Color.Parse("#FBFBFC"));
			app.Resources["CardServerAccent"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["CardModelsAccent"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["CardProfilesAccent"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["CardVersionAccent"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["CardModelAccent"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["CardServerLabel"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["CardModelsLabel"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["CardProfilesLabel"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["CardVersionLabel"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["CardModelLabel"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["CardSubText"] = new SolidColorBrush(Color.Parse("#6B7280"));
			app.Resources["GpuMonitorBackground"] = new SolidColorBrush(Color.Parse("#F4F5F7"));
			app.Resources["GpuMonitorInner"] = new SolidColorBrush(Color.Parse("#ECEEF1"));
			app.Resources["GpuMonitorHeader"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["ProgressTrack"] = new SolidColorBrush(Color.Parse("#D9DDE3"));
			app.Resources["ProgressVram"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["ProgressCore"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["ProgressPower"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["ProgressFan"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["ProgressRam"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["SidebarBackground"] = new SolidColorBrush(Color.Parse("#F0F1F3"));
			app.Resources["SidebarItemBackground"] = new SolidColorBrush(Color.Parse("#E5E7EB"));
			app.Resources["SidebarItemSelected"] = new SolidColorBrush(Color.Parse("#D8DADF"));
			app.Resources["SidebarItemText"] = new SolidColorBrush(Color.Parse("#5F636B"));
			app.Resources["SidebarItemTextSelected"] = new SolidColorBrush(Color.Parse("#34363B"));
			app.Resources["TabItemBackground"] = new SolidColorBrush(Color.Parse("#E5E7EB"));
			app.Resources["TabItemText"] = new SolidColorBrush(Color.Parse("#5F636B"));
			app.Resources["TabItemSelectedBackground"] = new SolidColorBrush(Color.Parse("#2F81F7"));
			app.Resources["TabItemSelectedText"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["TabItemHoverBackground"] = new SolidColorBrush(Color.Parse("#D8DADF"));
			app.Resources["ProfileInnerBackground"] = new SolidColorBrush(Color.Parse("#F6F8FA"));
			app.Resources["ProfileModelBackground"] = new SolidColorBrush(Color.Parse("#F6F8FA"));
			app.Resources["BadgeMtp"] = new SolidColorBrush(Color.Parse("#7C3AED"));
			app.Resources["BadgeReasoning"] = new SolidColorBrush(Color.Parse("#059669"));
			app.Resources["BadgeFlash"] = new SolidColorBrush(Color.Parse("#2563EB"));
			app.Resources["BadgeCache"] = new SolidColorBrush(Color.Parse("#D97706"));
			app.Resources["BadgeBatching"] = new SolidColorBrush(Color.Parse("#0891B2"));
			app.Resources["BadgeText"] = new SolidColorBrush(Color.Parse("#F0F2F5"));
			app.Resources["ThemeTextOnAccent"] = new SolidColorBrush(Color.Parse("#F0F2F5"));
			app.Resources["ServerBannerBackground"] = new SolidColorBrush(Color.Parse("#FBFBFC"));
			app.Resources["ServerBannerLabel"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["ServerBannerSub"] = new SolidColorBrush(Color.Parse("#6B7280"));
			app.Resources["SectionTitle"] = new SolidColorBrush(Color.Parse("#1F2328"));
			app.Resources["SectionLabel"] = new SolidColorBrush(Color.Parse("#393F46"));
			app.Resources["SectionLabelLight"] = new SolidColorBrush(Color.Parse("#656D76"));
			app.Resources["BtnPrimary"] = new SolidColorBrush(Color.Parse("#2F81F7"));
			app.Resources["BtnSuccess"] = new SolidColorBrush(Color.Parse("#2DA44E"));
			app.Resources["BtnDanger"] = new SolidColorBrush(Color.Parse("#CF222E"));
			app.Resources["BtnSecondary"] = new SolidColorBrush(Color.Parse("#E5E7EB"));
			app.Resources["BtnPurple"] = new SolidColorBrush(Color.Parse("#8B54FF"));
			app.Resources["BtnCyan"] = new SolidColorBrush(Color.Parse("#0891B2"));
		}
		else
		{
			app.Resources["ThemeBackground"] = new SolidColorBrush(Color.Parse("#0F172A"));
			app.Resources["ThemeSurface"] = new SolidColorBrush(Color.Parse("#1E293B"));
			app.Resources["ThemeCard"] = new SolidColorBrush(Color.Parse("#0F172A"));
			app.Resources["ThemeTextPrimary"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["ThemeTextSecondary"] = new SolidColorBrush(Color.Parse("#94A3B8"));
			app.Resources["ThemeTextTertiary"] = new SolidColorBrush(Color.Parse("#64748B"));
			app.Resources["ThemeBorder"] = new SolidColorBrush(Color.Parse("#334155"));
			app.Resources["ThemeBorderStrong"] = new SolidColorBrush(Color.Parse("#475569"));
			app.Resources["ThemeInputBackground"] = new SolidColorBrush(Color.Parse("#0F172A"));
			app.Resources["ThemeAccent"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["ThemeAccentHover"] = new SolidColorBrush(Color.Parse("#4F46E5"));
			app.Resources["CardServerBackground"] = new SolidColorBrush(Color.Parse("#1E1B4B"));
			app.Resources["CardModelsBackground"] = new SolidColorBrush(Color.Parse("#064E3B"));
			app.Resources["CardProfilesBackground"] = new SolidColorBrush(Color.Parse("#7C2D12"));
			app.Resources["CardVersionBackground"] = new SolidColorBrush(Color.Parse("#1E3A5F"));
			app.Resources["CardModelBackground"] = new SolidColorBrush(Color.Parse("#172554"));
			app.Resources["CardSectionBackground"] = new SolidColorBrush(Color.Parse("#1F1533"));
			app.Resources["CardServerAccent"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["CardModelsAccent"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["CardProfilesAccent"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["CardVersionAccent"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["CardModelAccent"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["CardServerLabel"] = new SolidColorBrush(Color.Parse("#818CF8"));
			app.Resources["CardModelsLabel"] = new SolidColorBrush(Color.Parse("#34D399"));
			app.Resources["CardProfilesLabel"] = new SolidColorBrush(Color.Parse("#FDBA74"));
			app.Resources["CardVersionLabel"] = new SolidColorBrush(Color.Parse("#60A5FA"));
			app.Resources["CardModelLabel"] = new SolidColorBrush(Color.Parse("#60A5FA"));
			app.Resources["CardSubText"] = new SolidColorBrush(Color.Parse("#A5B4FC"));
			app.Resources["GpuMonitorBackground"] = new SolidColorBrush(Color.Parse("#111A2A"));
			app.Resources["GpuMonitorInner"] = new SolidColorBrush(Color.Parse("#111A2A"));
			app.Resources["GpuMonitorHeader"] = new SolidColorBrush(Color.Parse("#A78BFA"));
			app.Resources["ProgressTrack"] = new SolidColorBrush(Color.Parse("#3A4352"));
			app.Resources["ProgressVram"] = new SolidColorBrush(Color.Parse("#3B82F6"));
			app.Resources["ProgressCore"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["ProgressPower"] = new SolidColorBrush(Color.Parse("#F59E0B"));
			app.Resources["ProgressFan"] = new SolidColorBrush(Color.Parse("#8B5CF6"));
			app.Resources["ProgressRam"] = new SolidColorBrush(Color.Parse("#10B981"));
			app.Resources["SidebarBackground"] = new SolidColorBrush(Color.Parse("#0F172A"));
			app.Resources["SidebarItemBackground"] = new SolidColorBrush(Color.Parse("#1E293B"));
			app.Resources["SidebarItemSelected"] = new SolidColorBrush(Color.Parse("#27272A"));
			app.Resources["SidebarItemText"] = new SolidColorBrush(Color.Parse("#94A3B8"));
			app.Resources["SidebarItemTextSelected"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["TabItemBackground"] = new SolidColorBrush(Color.Parse("#334155"));
			app.Resources["TabItemText"] = new SolidColorBrush(Color.Parse("#94A3B8"));
			app.Resources["TabItemSelectedBackground"] = new SolidColorBrush(Color.Parse("#6366F1"));
			app.Resources["TabItemSelectedText"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["TabItemHoverBackground"] = new SolidColorBrush(Color.Parse("#475569"));
			app.Resources["ProfileInnerBackground"] = new SolidColorBrush(Color.Parse("#2D1B69"));
			app.Resources["ProfileModelBackground"] = new SolidColorBrush(Color.Parse("#2D1B69"));
			app.Resources["BadgeMtp"] = new SolidColorBrush(Color.Parse("#7C3AED"));
			app.Resources["BadgeReasoning"] = new SolidColorBrush(Color.Parse("#059669"));
			app.Resources["BadgeFlash"] = new SolidColorBrush(Color.Parse("#2563EB"));
			app.Resources["BadgeCache"] = new SolidColorBrush(Color.Parse("#D97706"));
			app.Resources["BadgeBatching"] = new SolidColorBrush(Color.Parse("#0891B2"));
			app.Resources["BadgeText"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["ThemeTextOnAccent"] = new SolidColorBrush(Color.Parse("#F0F2F5"));
			app.Resources["ServerBannerBackground"] = new SolidColorBrush(Color.Parse("#1E1B4B"));
			app.Resources["ServerBannerLabel"] = new SolidColorBrush(Color.Parse("#818CF8"));
			app.Resources["ServerBannerSub"] = new SolidColorBrush(Color.Parse("#64748B"));
			app.Resources["SectionTitle"] = new SolidColorBrush(Color.Parse("White"));
			app.Resources["SectionLabel"] = new SolidColorBrush(Color.Parse("#E2E8F0"));
			app.Resources["SectionLabelLight"] = new SolidColorBrush(Color.Parse("#64748B"));
			app.Resources["BtnPrimary"] = new SolidColorBrush(Color.Parse("#4338CA"));
			app.Resources["BtnSuccess"] = new SolidColorBrush(Color.Parse("#059669"));
			app.Resources["BtnDanger"] = new SolidColorBrush(Color.Parse("#DC2626"));
			app.Resources["BtnSecondary"] = new SolidColorBrush(Color.Parse("#475569"));
			app.Resources["BtnPurple"] = new SolidColorBrush(Color.Parse("#7C3AED"));
			app.Resources["BtnCyan"] = new SolidColorBrush(Color.Parse("#0891B2"));
		}
	}

	public App()
	{
		_0021XamlIlPopulateTrampoline(this);
	}

	private unsafe static void _0021XamlIlPopulate(IServiceProvider? P_0, App? P_1)
	{
		CompiledAvaloniaXaml.XamlIlContext.Context<App> context = new CompiledAvaloniaXaml.XamlIlContext.Context<App>(P_0, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FApp_002Eaxaml.Singleton }, "avares://LlamaStudio/App.axaml")
		{
			RootObject = P_1,
			IntermediateRoot = P_1
		};
		App app2;
		App app = (app2 = P_1);
		context.PushParent(app2);
		if (app2.Resources is ResourceDictionary resourceDictionary)
		{
			resourceDictionary.EnsureCapacity(resourceDictionary.Count + 116);
		}
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_1), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeSurface", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_2), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeCard", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_3), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeTextPrimary", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_4), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeTextSecondary", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_5), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeTextTertiary", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_6), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeBorder", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_7), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeBorderStrong", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_8), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeInputBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_9), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeAccent", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_10), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ThemeAccentHover", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_11), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardServerBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_12), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardModelsBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_13), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardProfilesBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_14), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardVersionBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_15), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardModelBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_16), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardSectionBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_17), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardServerAccent", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_18), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardModelsAccent", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_19), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardProfilesAccent", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_20), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardVersionAccent", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_21), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardModelAccent", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_22), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardServerLabel", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_23), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardModelsLabel", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_24), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardProfilesLabel", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_25), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardVersionLabel", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_26), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardModelLabel", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_27), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"CardSubText", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_28), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"GpuMonitorBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_29), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"GpuMonitorInner", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_30), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"GpuMonitorHeader", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_31), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ProgressTrack", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_32), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ProgressVram", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_33), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ProgressCore", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_34), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ProgressPower", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_35), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ProgressFan", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_36), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ProgressRam", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_37), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SidebarBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_38), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SidebarItemBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_39), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SidebarItemSelected", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_40), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SidebarItemText", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_41), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SidebarItemTextSelected", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_42), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"TabItemBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_43), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"TabItemText", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_44), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"TabItemSelectedBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_45), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"TabItemSelectedText", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_46), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"TabItemHoverBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_47), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ProfileInnerBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_48), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ProfileModelBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_49), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BadgeMtp", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_50), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BadgeReasoning", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_51), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BadgeFlash", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_52), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BadgeCache", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_53), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BadgeBatching", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_54), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BadgeText", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_55), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ServerBannerBackground", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_56), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ServerBannerLabel", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_57), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ServerBannerSub", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_58), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SectionTitle", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_59), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SectionLabel", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_60), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SectionLabelLight", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_61), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BtnPrimary", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_62), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BtnSuccess", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_63), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BtnDanger", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_64), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BtnSecondary", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_65), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BtnPurple", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_66), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BtnCyan", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_67), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"EqualConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_68), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BoolToStringConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_69), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"GreaterThanConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_70), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"NotNullConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_71), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"InvertedBoolConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_72), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"IntToBoolConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_73), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"NotEqualConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_74), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"StringEqualBrushConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_75), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SizeFormatConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_76), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"LogLevelBrushConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_77), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BuildTypeToBrushConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_78), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"PrereleaseBorderConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_79), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ContainsConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_80), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"NotContainsConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_81), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ChatRoleToBrushConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_82), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ChatRoleToInitialConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_83), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ChatRoleToNameConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_84), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ChatRoleTextBrushConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_85), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"NullVisibilityConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_86), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ServerConnectionStatusConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_87), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ServerStateBrushConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_88), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"HasItemsVisibilityConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_89), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"Base64ToImageConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_90), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"McpTransportToVisibilityConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_91), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"McpConnectedToBrushConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_92), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BookmarkBrushConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_93), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BookmarkTooltipConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_94), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ChatRoleToVisibilityConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_95), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ReasoningVisibilityConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_96), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ToolResultLengthConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_97), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ReasoningChevronConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_98), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"McpChipBgConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_99), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"McpChipFgConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_100), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"McpChipOpacityConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_101), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"McpToggleBgConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_102), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"McpToggleFgConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_103), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"ContextProgressBarColorConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_104), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"SessionSelectedBgConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_105), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"BoolToIndexConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_106), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"EqualsZeroConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_107), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"IntTextConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_108), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"FloatTextConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_109), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"StringTextConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_110), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"DoubleTextConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_111), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"DefaultToOpacityConverter", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_112), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"IconCopy", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_113), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"IconEdit", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_114), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"IconRegenerate", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_115), context));
		((ResourceDictionary)app2.Resources).AddDeferred((object)"IconDelete", XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<object>((nint)(delegate*<IServiceProvider?, object?>)(&XamlClosure_1.Build_116), context));
		app2.DataTemplates.Add(new ViewLocator());
		app2.Styles.Add(new FluentTheme(context));
		app2.Styles.Add(new MaterialIconStyles(context));
		Styles styles = app2.Styles;
		Style style = new Style();
		style.Selector = ((Selector?)null).OfType(typeof(TextBlock));
		Setter setter = new Setter();
		setter.Property = TextBlock.FontFamilyProperty;
		setter.Value = new FontFamily(((IUriContext)context).BaseUri, "Segoe UI, Arial, sans-serif");
		style.Add(setter);
		styles.Add(style);
		Styles styles2 = app2.Styles;
		Style style2 = new Style();
		style2.Selector = ((Selector?)null).OfType(typeof(ListBox)).Child().OfType(typeof(ListBoxItem));
		Setter setter2 = new Setter();
		setter2.Property = TemplatedControl.BackgroundProperty;
		setter2.Value = new ImmutableSolidColorBrush(16777215u);
		style2.Add(setter2);
		Setter setter3 = new Setter();
		setter3.Property = TemplatedControl.PaddingProperty;
		setter3.Value = new Thickness(8.0, 4.0, 8.0, 4.0);
		style2.Add(setter3);
		Setter setter4 = new Setter();
		setter4.Property = TemplatedControl.CornerRadiusProperty;
		setter4.Value = new CornerRadius(6.0, 6.0, 6.0, 6.0);
		style2.Add(setter4);
		styles2.Add(style2);
		Styles styles3 = app2.Styles;
		Style style3;
		Style item = (style3 = new Style());
		context.PushParent(style3);
		Style style4 = style3;
		style4.Selector = ((Selector?)null).OfType(typeof(ListBox)).Child().OfType(typeof(ListBoxItem))
			.Class(":selected");
		Setter setter6;
		Setter setter5 = (setter6 = new Setter());
		context.PushParent(setter6);
		Setter setter7 = setter6;
		setter7.Property = TemplatedControl.BackgroundProperty;
		context.ProvideTargetProperty = CompiledAvaloniaXaml.XamlIlHelpers.Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Property();
		IBinding value = new DynamicResourceExtension("ThemeInputBackground").ProvideValue(context);
		context.ProvideTargetProperty = null;
		setter7.Value = value;
		context.PopParent();
		style4.Add(setter5);
		context.PopParent();
		styles3.Add(item);
		Styles styles4 = app2.Styles;
		Style item2 = (style3 = new Style());
		context.PushParent(style3);
		Style style5 = style3;
		style5.Selector = ((Selector?)null).OfType(typeof(ListBox)).Child().OfType(typeof(ListBoxItem))
			.Class(":pointerover");
		Setter setter8 = (setter6 = new Setter());
		context.PushParent(setter6);
		Setter setter9 = setter6;
		setter9.Property = TemplatedControl.BackgroundProperty;
		context.ProvideTargetProperty = CompiledAvaloniaXaml.XamlIlHelpers.Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Property();
		IBinding value2 = new DynamicResourceExtension("ThemeInputBackground").ProvideValue(context);
		context.ProvideTargetProperty = null;
		setter9.Value = value2;
		context.PopParent();
		style5.Add(setter8);
		context.PopParent();
		styles4.Add(item2);
		Styles styles5 = app2.Styles;
		Style style6 = new Style();
		style6.Selector = ((Selector?)null).OfType(typeof(Button)).Class("help-icon-btn");
		Setter setter10 = new Setter();
		setter10.Property = Layoutable.WidthProperty;
		setter10.Value = 20.0;
		style6.Add(setter10);
		Setter setter11 = new Setter();
		setter11.Property = Layoutable.HeightProperty;
		setter11.Value = 20.0;
		style6.Add(setter11);
		Setter setter12 = new Setter();
		setter12.Property = TemplatedControl.CornerRadiusProperty;
		setter12.Value = new CornerRadius(10.0, 10.0, 10.0, 10.0);
		style6.Add(setter12);
		Setter setter13 = new Setter();
		setter13.Property = TemplatedControl.BackgroundProperty;
		setter13.Value = new ImmutableSolidColorBrush(1667690784u);
		style6.Add(setter13);
		styles5.Add(style6);
		Styles styles6 = app2.Styles;
		Style style7 = new Style();
		style7.Selector = ((Selector?)null).OfType(typeof(Button)).Class("help-icon-btn").Class(":pointerover");
		Setter setter14 = new Setter();
		setter14.Property = TemplatedControl.BackgroundProperty;
		setter14.Value = new ImmutableSolidColorBrush(1667690832u);
		style7.Add(setter14);
		styles6.Add(style7);
		Styles styles7 = app2.Styles;
		Style style8 = new Style();
		style8.Selector = ((Selector?)null).OfType(typeof(ToolTip));
		Setter setter15 = new Setter();
		setter15.Property = Layoutable.MaxWidthProperty;
		setter15.Value = 480.0;
		style8.Add(setter15);
		Setter setter16 = new Setter();
		setter16.Property = TemplatedControl.PaddingProperty;
		setter16.Value = new Thickness(14.0, 12.0, 14.0, 12.0);
		style8.Add(setter16);
		Setter setter17 = new Setter();
		setter17.Property = TemplatedControl.CornerRadiusProperty;
		setter17.Value = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		style8.Add(setter17);
		Setter setter18 = new Setter();
		setter18.Property = TemplatedControl.BackgroundProperty;
		setter18.Value = new ImmutableSolidColorBrush(4280166715u);
		style8.Add(setter18);
		Setter setter19 = new Setter();
		setter19.Property = TemplatedControl.BorderBrushProperty;
		setter19.Value = new ImmutableSolidColorBrush(4284704497u);
		style8.Add(setter19);
		Setter setter20 = new Setter();
		setter20.Property = TemplatedControl.BorderThicknessProperty;
		setter20.Value = new Thickness(1.5, 1.5, 1.5, 1.5);
		style8.Add(setter20);
		styles7.Add(style8);
		Styles styles8 = app2.Styles;
		Style style9 = new Style();
		style9.Selector = ((Selector?)null).OfType(typeof(ToolTip)).Descendant().OfType(typeof(TextBlock));
		Setter setter21 = new Setter();
		setter21.Property = TextBlock.FontSizeProperty;
		setter21.Value = 12.0;
		style9.Add(setter21);
		Setter setter22 = new Setter();
		setter22.Property = TextBlock.ForegroundProperty;
		setter22.Value = new ImmutableSolidColorBrush(4293060848u);
		style9.Add(setter22);
		Setter setter23 = new Setter();
		setter23.Property = TextBlock.FontFamilyProperty;
		setter23.Value = new FontFamily(((IUriContext)context).BaseUri, "Segoe UI, Arial, sans-serif");
		style9.Add(setter23);
		Setter setter24 = new Setter();
		setter24.Property = TextBlock.TextWrappingProperty;
		setter24.Value = TextWrapping.Wrap;
		style9.Add(setter24);
		styles8.Add(style9);
		Styles styles9 = app2.Styles;
		Style item3 = (style3 = new Style());
		context.PushParent(style3);
		Style style10 = style3;
		style10.Selector = ((Selector?)null).OfType(typeof(Button)).Class("accent");
		Setter setter25 = (setter6 = new Setter());
		context.PushParent(setter6);
		Setter setter26 = setter6;
		setter26.Property = TemplatedControl.BackgroundProperty;
		context.ProvideTargetProperty = CompiledAvaloniaXaml.XamlIlHelpers.Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Property();
		IBinding value3 = new DynamicResourceExtension("ThemeAccent").ProvideValue(context);
		context.ProvideTargetProperty = null;
		setter26.Value = value3;
		context.PopParent();
		style10.Add(setter25);
		Setter setter27 = new Setter();
		setter27.Property = TemplatedControl.ForegroundProperty;
		setter27.Value = new ImmutableSolidColorBrush(uint.MaxValue);
		style10.Add(setter27);
		Setter setter28 = new Setter();
		setter28.Property = TemplatedControl.BorderThicknessProperty;
		setter28.Value = new Thickness(0.0, 0.0, 0.0, 0.0);
		style10.Add(setter28);
		Setter setter29 = new Setter();
		setter29.Property = TemplatedControl.FontWeightProperty;
		setter29.Value = FontWeight.DemiBold;
		style10.Add(setter29);
		context.PopParent();
		styles9.Add(item3);
		Styles styles10 = app2.Styles;
		Style item4 = (style3 = new Style());
		context.PushParent(style3);
		Style style11 = style3;
		style11.Selector = ((Selector?)null).OfType(typeof(Button)).Class("accent").Class(":pointerover");
		Setter setter30 = (setter6 = new Setter());
		context.PushParent(setter6);
		Setter setter31 = setter6;
		setter31.Property = TemplatedControl.BackgroundProperty;
		context.ProvideTargetProperty = CompiledAvaloniaXaml.XamlIlHelpers.Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Property();
		IBinding value4 = new DynamicResourceExtension("ThemeAccentHover").ProvideValue(context);
		context.ProvideTargetProperty = null;
		setter31.Value = value4;
		context.PopParent();
		style11.Add(setter30);
		context.PopParent();
		styles10.Add(item4);
		Styles styles11 = app2.Styles;
		Style style12 = new Style();
		style12.Selector = ((Selector?)null).OfType(typeof(Button)).Class("accent").Class(":pressed");
		Setter setter32 = new Setter();
		setter32.Property = Visual.OpacityProperty;
		setter32.Value = 0.8;
		style12.Add(setter32);
		styles11.Add(style12);
		context.PopParent();
		context.AvaloniaNameScope.Complete();
	}

	private static void _0021XamlIlPopulateTrampoline(App? P_0)
	{
		if (_0021XamlIlPopulateOverride != null)
		{
			_0021XamlIlPopulateOverride(P_0);
		}
		else
		{
			_0021XamlIlPopulate(XamlIlRuntimeHelpers.CreateRootServiceProviderV3(null), P_0);
		}
	}	// r96: show a startup failure the user can actually see (a windowless
	// process in Task Manager is useless for diagnosis). On Windows the native
	// MessageBox works even when the Avalonia UI failed to come up.
	private static void ShowStartupFailure(string context, Exception exception)
	{
		string details = $"LlamaStudio не удалось открыть окно: {context}.\n\n{exception.GetType().Name}: {exception.Message}\n\nПодробности: %TEMP%\\LlamaStudio-recovered.log";
		if (OperatingSystem.IsWindows())
		{
			try
			{
				StartupMessageBoxW(IntPtr.Zero, details, "LlamaStudio — ошибка запуска", 0x10u);
			}
			catch
			{
			}
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int StartupMessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}