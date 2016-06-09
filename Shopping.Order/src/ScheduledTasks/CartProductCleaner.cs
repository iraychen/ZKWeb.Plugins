﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZKWeb.Logging;
using ZKWeb.Plugins.Common.Base.src.Model;
using ZKWeb.Plugins.Common.Base.src.Repositories;
using ZKWeb.Plugins.Shopping.Order.src.Database;
using ZKWeb.Utils.IocContainer;

namespace ZKWeb.Plugins.Shopping.Order.src.ScheduledTasks {
	/// <summary>
	/// 清理过期的购物车商品
	/// </summary>
	[ExportMany]
	public class CartProductCleaner : IScheduledTaskExecutor {
		/// <summary>
		/// 任务键名
		/// </summary>
		public string Key { get { return "Shopping.Order.CartProductCleaner"; } }

		/// <summary>
		/// 每小时执行一次
		/// </summary>
		public bool ShouldExecuteNow(DateTime lastExecuted) {
			return ((DateTime.UtcNow - lastExecuted).TotalHours > 1.0);
		}

		/// <summary>
		/// 自动确认订单
		/// </summary>
		public void Execute() {
			var count = UnitOfWork.WriteData<CartProduct, long>(r => {
				var now = DateTime.UtcNow;
				return r.DeleteWhere(p => p.ExpireTime < now);
			});
			var logManager = Application.Ioc.Resolve<LogManager>();
			logManager.LogInfo(string.Format(
				"CartProductCleaner executed, {0} cart products removed", count));
		}
	}
}