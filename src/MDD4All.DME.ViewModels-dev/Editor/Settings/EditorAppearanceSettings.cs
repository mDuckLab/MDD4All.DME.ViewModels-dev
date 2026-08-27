namespace MDD4All.DME.ViewModels.Editor.Settings
{
    public class EditorAppearanceSettings
    {
        public bool TintEnabled { get; set; } = true;

        public int MaxDepth { get; set; } = 5;

        public bool ShowIcons { get; set; } = true;

        public bool ShowIndexNumbers { get; set; } = true;

        public bool ShowReadOnlyBadges { get; set; } = true;

        public bool ShowTypeBadges { get; set; } = true;

        // Labels from the data model's [Display] annotations, rather than the property
        // names the code uses. On by default - that is what the annotations are for.
        public bool ShowAnnotationNames { get; set; } = true;
    }
}
