﻿using System;
using System.Collections.Generic;
using System.Linq;
using ZKWeb.Cache;
using ZKWeb.Plugins.Common.Base.src.Repositories;
using ZKWeb.Plugins.Finance.Payment.src.Database;
using ZKWeb.Plugins.Finance.Payment.src.Model;
using ZKWeb.Server;
using ZKWebStandard.Collections;
using ZKWebStandard.Extensions;
using ZKWebStandard.Ioc;

namespace ZKWeb.Plugins.Finance.Payment.src.Managers {
	/// <summary>
	/// 支付接口管理器
	/// </summary>
	[ExportMany, SingletonReuse]
	public class PaymentApiManager : ICacheCleaner {
		/// <summary>
		/// 支付接口的缓存时间
		/// 默认是3秒，可通过网站配置指定
		/// </summary>
		protected TimeSpan PaymentApiCacheTime { get; set; }
		/// <summary>
		/// 支付接口的缓存
		/// { 接口Id: 接口, ... }
		/// </summary>
		protected MemoryCache<long, PaymentApi> PaymentApiCache { get; set; }
		/// <summary>
		/// 支付接口列表的缓存
		/// { (所有人Id, 交易类型): 接口, ... }
		/// </summary>
		protected MemoryCache<Pair<long, string>, IList<PaymentApi>> PaymentApisCache { get; set; }

		/// <summary>
		/// 初始化
		/// </summary>
		public PaymentApiManager() {
			var configManager = Application.Ioc.Resolve<ConfigManager>();
			PaymentApiCacheTime = TimeSpan.FromSeconds(
				configManager.WebsiteConfig.Extra.GetOrDefault(ExtraConfigKeys.PaymentApiCacheTime, 3));
			PaymentApiCache = new MemoryCache<long, PaymentApi>();
			PaymentApisCache = new MemoryCache<Pair<long, string>, IList<PaymentApi>>();
		}

		/// <summary>
		/// 获取支付接口
		/// 不存在或已删除时返回null
		/// 结果会按接口Id缓存一定时间
		/// </summary>
		/// <param name="apiId">支付接口的Id</param>
		/// <returns></returns>
		public virtual PaymentApi GetPaymentApi(long apiId) {
			// 从缓存获取
			var api = PaymentApiCache.GetOrDefault(apiId);
			if (api != null) {
				return api;
			}
			// 从数据库获取
			UnitOfWork.ReadData<PaymentApi>(r => {
				api = r.GetByIdWhereNotDeleted(apiId);
				// 保存到缓存
				if (api != null) {
					PaymentApiCache.Put(apiId, api, PaymentApiCacheTime);
				}
			});
			return api;
		}

		/// <summary>
		/// 获取支付接口列表
		/// 结果会按所有人Id和交易类型缓存一定时间
		/// </summary>
		/// <param name="ownerId">所有人Id，传入null时获取后台添加的支付接口列表</param>
		/// <param name="transactionType">交易类型</param>
		/// <returns></returns>
		public virtual IList<PaymentApi> GetPaymentApis(long? ownerId, string transactionType) {
			// 从缓存获取
			var key = Pair.Create(ownerId ?? 0, transactionType);
			var apis = PaymentApisCache.GetOrDefault(key);
			if (apis != null) {
				return apis;
			}
			// 从数据库获取
			apis = UnitOfWork.ReadData<PaymentApi, List<PaymentApi>>(r => {
				return r.GetMany(a => a.Owner.Id == ownerId && !a.Deleted)
					.OrderBy(a => a.DisplayOrder).ToList()
					.Where(a => a.SupportTransactionTypes.Contains(transactionType)).ToList();
			});
			// 保存到缓存
			PaymentApisCache.Put(key, apis, PaymentApiCacheTime);
			return apis;
		}

		/// <summary>
		/// 清理缓存
		/// </summary>
		public void ClearCache() {
			PaymentApiCache.Clear();
			PaymentApisCache.Clear();
		}
	}
}
