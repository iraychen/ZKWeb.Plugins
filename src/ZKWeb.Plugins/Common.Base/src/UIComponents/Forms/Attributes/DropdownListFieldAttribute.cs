﻿using System;

namespace ZKWeb.Plugins.Common.Base.src.UIComponents.Forms.Attributes {
	/// <summary>
	/// 下拉列表
	/// </summary>
	public class DropdownListFieldAttribute : FormFieldAttribute {
		/// <summary>
		/// 选项来源，必须继承IListItemProvider
		/// </summary>
		public Type Source { get; set; }

		/// <summary>
		/// 初始化
		/// </summary>
		/// <param name="name">字段名称</param>
		/// <param name="source">选项来源，必须继承IListItemProvider</param>
		public DropdownListFieldAttribute(string name, Type source) {
			Name = name;
			Source = source;
		}
	}
}
