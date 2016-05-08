﻿using DryIoc;
using DryIocAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ZKWeb.Plugins.Common.Base.src.Managers;
using ZKWeb.Plugins.Common.Region.src.Config;
using ZKWeb.Plugins.Common.Region.src.ListItemProviders;
using ZKWeb.Plugins.Common.Region.src.Managers;
using ZKWeb.Utils.Extensions;
using ZKWeb.Utils.Functions;
using ZKWeb.Web.ActionResults;
using ZKWeb.Web.Interfaces;

namespace ZKWeb.Plugins.Common.Region.src.Controllers {
	/// <summary>
	/// 地区使用的Api控制器
	/// </summary>
	[ExportMany]
	public class ApiController : IController {
		/// <summary>
		/// 获取国家信息
		/// 返回默认国家和国家列表
		/// </summary>
		/// <returns></returns>
		[Action("api/region/country_info", HttpMethods.POST)]
		public IActionResult CountryInfo() {
			var configManager = Application.Ioc.Resolve<GenericConfigManager>();
			var settings = configManager.GetData<RegionSettings>();
			var defaultCountry = settings.DefaultCountry;
			var countries = new CountryListItemProvider().GetItems().ToList();
			return new JsonResult(new { defaultCountry, countries });
		}

		/// <summary>
		/// 获取指定国家的地区树
		/// </summary>
		/// <returns></returns>
		[Action("api/region/region_tree_from_country", HttpMethods.POST)]
		public IActionResult RegionTreeFromCountry() {
			var countryName = HttpContextUtils.CurrentContext.Request.Get<string>("country");
			var regionManager = Application.Ioc.Resolve<RegionManager>();
			var country = regionManager.GetCountry(countryName);
			var tree = country == null ? null : country.GetRegionsTree();
			return new JsonResult(new { tree });
		}
	}
}