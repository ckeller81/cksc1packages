using System;
using Composite.Data;
using Composite.Data.Hierarchy;
using Composite.Data.Hierarchy.DataAncestorProviders;
using Composite.Data.Types;

namespace CkSoftware.ImageWatermark.DataTypes
{
	[AutoUpdateble]
	[KeyPropertyName("Id")]
	[DataScope(DataScopeIdentifier.PublicName)]
	[DataAncestorProvider(typeof(NoAncestorDataAncestorProvider))]
	[ImmutableTypeId("{7CDB443F-ECF8-4EA0-AE4A-FFB3FE1622A2}")]
	[LabelPropertyName("Name")]
	public interface IWatermarkConfiguration : IData
	{
		[StoreFieldType(PhysicalStoreFieldType.Guid)]
		[ImmutableFieldId("{172DD44C-426B-4812-834B-6B45366E78CB}")]
		Guid Id { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.String, 254)]
		[ImmutableFieldId("{77E63773-A384-440A-AED3-84798BF29D32}")]
		string Name { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.Integer)]
		[ImmutableFieldId("{28BA44A7-18DF-40B1-88A3-F07FAB7A7F36}")]
		[DefaultFieldIntValue(15)]
		int FontSize { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.Boolean)]
		[ImmutableFieldId("{13C1CDBE-D5DB-4880-B14C-D5751657EAEB}")]
		bool ShowOnTopLeft { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.Boolean)]
		[ImmutableFieldId("{5951C77D-8D80-425E-BA08-15818C3792DB}")]
		bool ShowOnTopRight { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.Boolean)]
		[ImmutableFieldId("{E5DF9933-4C4B-40C8-939F-7B8523B9A1A3}")]
		[DefaultFieldBoolValue(true)]
		bool ShowOnBottmRight { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.Boolean)]
		[ImmutableFieldId("{EA7D391B-525E-44C7-B40E-82F09CA408D2}")]
		bool ShowOnBottmLeft { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.String, 254)]
		[ImmutableFieldId("{E8AD4ADF-A062-4347-8AD7-0EE514738504}")]
		string WatermarkText { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.String, 2048, IsNullable = true)]
		[ImmutableFieldId("{D478437D-050B-44A8-B29B-21F473A26E7A}")]
		[ForeignKey(typeof(IMediaFile), "KeyPath", AllowCascadeDeletes = false, NullReferenceValueType = typeof(string), NullReferenceValue = null)]
		string WatermarkImageFilePath { get; set; }

		[StoreFieldType(PhysicalStoreFieldType.String, 2048, IsNullable = true)]
		[ImmutableFieldId("{26E9268F-5BA5-4A6F-AC1C-0990EECFB564}")]
		[ForeignKey(typeof(IMediaFileFolder), "KeyPath", AllowCascadeDeletes = false, NullReferenceValue = null, NullReferenceValueType = typeof(string))]
		string TargetMediaFolderPath { get; set; }
	}
}
