namespace Eventus.Runtime.Core
{
    public static class EditorMessages
    {
        public const string WindowTitle = "Eventus";
        public const string ToolbarTabNotfound = "Toolbar not found components.";
        public const string UnsavedTitle = " - Unsaved";

        public const string ConfirmDeleteTitle = "Confirm Deletion";

        public const string ConfirmDeleteBody =
            "Are you sure you want to remove the channel '{0}'? This action cannot be undone until the next save.";

        public const string ErrorTitle = "Error";
        public const string SuccessTitle = "Success";

        public const string ErrorNameEmpty = "The channel name cannot be empty or contain only whitespace.";

        public const string ErrorNameInvalidChar =
            "The channel name must start with a letter and can only contain letters and numbers.";

        public const string ErrorAttributesInvalid =
            "To create a valid channel you must choose a main attribute type (Event/Data).";

        public const string ErrorNameExists = "A channel with this name already exists in the list.";

        public const string ErrorNameInvalidToEnum =
            "The enum does not accept this type of character, use only letters, numbers and simple dashes.";

        public const string ErrorFindChannelScript =
            "Could not find 'Channel.cs'. Ensure the script exists at 'Eventus/Runtime/Generated/Channel.cs'.";

        public const string ErrorFileSaveFailed =
            "An error occurred while saving 'Channel.cs'. Please check the console for more details and contact support if the issue persists.";

        public const string ErrorCodeRecompile =
            "Eventus did not find any changes or data to recompile.";

        public const string SuccessFileSave =
            "'Channel.cs' has been updated successfully. Unity will now recompile the scripts.";
        
        public const string CategoryEmptyName =
            "Category name cannot be empty.";
        
        public const string CategoryNameExists =
            "Category already exists.";
        
        public const string RemoveCategoryTitle =
            "Remove Category?";
        
        public const string ConfirmCategoryDeleteBody =
            "Are you sure you want to remove '{0}'?";
    }
}