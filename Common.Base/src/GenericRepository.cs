﻿using DryIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ZKWeb.Core;
using ZKWeb.Plugins.Common.Base.src.TypeTraits;
using ZKWeb.Utils.Extensions;
using ZKWeb.Utils.Functions;

namespace ZKWeb.Plugins.Common.Base.src {
	/// <summary>
	/// 通用的数据仓储
	/// </summary>
	public class GenericRepository<TData>
		where TData : class {
		/// <summary>
		/// Id成员名称
		/// </summary>
		public virtual string IdMember { get { return "Id"; } }
		/// <summary>
		/// Id成员类型
		/// </summary>
		public virtual Type IdType { get { return typeof(TData).GetProperty(IdMember).PropertyType; } }
		/// <summary>
		/// 数据库上下文，需要在创建后设置
		/// </summary>
		public virtual DatabaseContext Context { get; set; }

		/// <summary>
		/// 获取满足条件的单个对象，找不到时返回null
		/// </summary>
		/// <param name="expression">查询条件</param>
		/// <returns></returns>
		public virtual TData Get(Expression<Func<TData, bool>> expression) {
			return Context.Get(expression);
		}

		/// <summary>
		/// 保存数据
		/// </summary>
		/// <param name="data">数据</param>
		/// <param name="update">更新数据的函数</param>
		public virtual void Save(ref TData data, Action<TData> update = null) {
			Context.Save(ref data, update);
		}

		/// <summary>
		/// 删除数据
		/// </summary>
		/// <param name="data">数据</param>
		public virtual void Delete(TData data) {
			Context.Delete(data);
		}

		/// <summary>
		/// 批量删除 返回删除的数量
		/// </summary>
		/// <param name="expression">删除条件</param>
		/// <returns></returns>
		public virtual long DeleteWhere(Expression<Func<TData, bool>> expression) {
			return Context.DeleteWhere(expression);
		}

		/// <summary>
		/// 获取满足条件的对象数量
		/// </summary>
		/// <param name="expression">表达式</param>
		/// <returns></returns>
		public virtual long Count(Expression<Func<TData, bool>> expression) {
			return Context.Count(expression);
		}

		/// <summary>
		/// 获取指定Id的单个对象，找不到时返回null
		/// </summary>
		/// <param name="id">数据Id</param>
		/// <returns></returns>
		public virtual TData GetById(object id) {
			id = id.ConvertOrDefault(IdType, null);
			return Get(ExpressionUtils.MakeMemberEqualiventExpression<TData>(IdMember, id));
		}

		/// <summary>
		/// 批量删除
		/// 返回删除的数量
		/// 这个函数仅设置Deleted为true，不会从数据库中删除
		/// </summary>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="idList">Id列表</param>
		public virtual long BatchDelete(IEnumerable<object> idList) {
			var databaseManager = Application.Ioc.Resolve<DatabaseManager>();
			var setter = ReflectionUtils.MakeSetter<TData, bool>(IsRecyclable.PropertyName);
			long count = 0;
			foreach (var id in idList) {
				var data = Context.Get(ExpressionUtils.MakeMemberEqualiventExpression<TData>(IdMember, id));
				if (data != null) {
					Context.Save(ref data, d => setter(d, true));
					++count;
				}
			}
			return count;
		}

		/// <summary>
		/// 批量恢复
		/// 返回恢复的数量
		/// </summary>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="idList">Id列表</param>
		/// <returns></returns>
		public virtual long BatchRecover(IEnumerable<object> idList) {
			var databaseManager = Application.Ioc.Resolve<DatabaseManager>();
			var setter = ReflectionUtils.MakeSetter<TData, bool>(IsRecyclable.PropertyName);
			long count = 0;
			foreach (var id in idList) {
				var data = Context.Get(ExpressionUtils.MakeMemberEqualiventExpression<TData>(IdMember, id));
				if (data != null) {
					Context.Save(ref data, d => setter(d, false));
					++count;
				}
			}
			return count;
		}

		/// <summary>
		/// 批量永久删除
		/// 返回删除的数量
		/// 这个函数会把数据从数据库中删除
		/// </summary>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="idList">Id列表</param>
		/// <returns></returns>
		public virtual long BatchDeleteForever(IEnumerable<object> idList) {
			var databaseManager = Application.Ioc.Resolve<DatabaseManager>();
			var setter = ReflectionUtils.MakeSetter<TData, bool>(IsRecyclable.PropertyName);
			long count = 0;
			foreach (var id in idList) {
				count += Context.DeleteWhere(
					ExpressionUtils.MakeMemberEqualiventExpression<TData>(IdMember, id));
			}
			return count;
		}
	}

	/// <summary>
	/// 数据仓储的静态函数类
	/// 用于提供简单的获取数据仓储和管理数据库上下文的函数
	/// UnitOfWork = Get Context + Get Repository + Execute Action + Commit (optional)
	/// 
	/// 注意UnitOfWork并没有使用全局变量，不支持嵌套
	/// 
	/// 在同一工作中使用多个仓储的例子
	/// GenericRepository.UnitOfWork[User](userRepository {
	///		var roleRepository = GenericRepository.Resolve[Role](userRepository.Context);
	/// })
	/// </summary>
	public static class GenericRepository {
		/// <summary>
		/// 获取指定数据的数据仓储
		/// 如果有在Ioc中注册的继承类，例如
		/// [ExportMany]
		/// class UserRepository : GenericRepository[TData] { }
		/// 则返回这个类，否则使用默认的仓储对象
		/// </summary>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="context">数据库上下文</param>
		/// <returns></returns>
		public static GenericRepository<TData> Resolve<TData>(DatabaseContext context)
			where TData : class {
			var repository = Application.Ioc.Resolve<GenericRepository<TData>>(IfUnresolved.ReturnDefault);
			repository = repository ?? new GenericRepository<TData>();
			repository.Context = context;
			return repository;
		}

		/// <summary>
		/// 获取指定类型的数据仓储
		/// </summary>
		/// <typeparam name="TRepository">数据仓储类型</typeparam>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="context">数据库上下文</param>
		/// <returns></returns>
		public static TRepository Resolve<TRepository, TData>(DatabaseContext context)
			where TRepository : GenericRepository<TData>
			where TData : class {
			var repository = Application.Ioc.Resolve<TRepository>();
			repository.Context = context;
			return repository;
		}

		/// <summary>
		/// 创建指定的数据仓库并执行工作
		/// </summary>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="action">工作内容</param>
		public static void UnitOfWork<TData>(Action<GenericRepository<TData>> action)
			where TData : class {
			var databaseManager = Application.Ioc.Resolve<DatabaseManager>();
			using (var context = databaseManager.GetContext()) {
				var repository = Resolve<TData>(context);
				action(repository);
			}
		}

		/// <summary>
		/// 创建指定的数据仓库并执行工作
		/// </summary>
		/// <typeparam name="TRepository">数据仓储类型</typeparam>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="action">工作内容</param>
		public static void UnitOfWork<TRepository, TData>(Action<TRepository> action)
			where TRepository : GenericRepository<TData>
			where TData : class {
			var databaseManager = Application.Ioc.Resolve<DatabaseManager>();
			using (var context = databaseManager.GetContext()) {
				var repository = Resolve<TRepository, TData>(context);
				action(repository);
			}
		}

		/// <summary>
		/// 创建指定的储存仓库并执行工作，执行后提交修改
		/// </summary>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="action">工作内容</param>
		public static void UnitOfWorkMayChangeData<TData>(Action<GenericRepository<TData>> action)
			where TData : class {
			UnitOfWork<TData>(repository => {
				action(repository);
				repository.Context.SaveChanges();
			});
		}

		/// <summary>
		/// 创建指定的储存仓库并执行工作，执行后提交修改
		/// </summary>
		/// <typeparam name="TRepository">数据仓储类型</typeparam>
		/// <typeparam name="TData">数据类型</typeparam>
		/// <param name="action">工作内容</param>
		public static void UnitOfWorkMayChangeData<TRepository, TData>(Action<TRepository> action)
			where TRepository : GenericRepository<TData>
			where TData : class {
			UnitOfWork<TRepository, TData>(repository => {
				action(repository);
				repository.Context.SaveChanges();
			});
		}
	}
}
