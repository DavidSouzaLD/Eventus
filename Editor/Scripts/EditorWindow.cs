using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Eventus.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eventus.Editor
{
    public class EditorWindow : UnityEditor.EditorWindow
    {
        #region Classes
        
        public class ToolbarsTab
        {
            public readonly string tabName;
            public Vector2 minWindowSize;
            public int index;
            public Button tabButton;
            public VisualElement tabContent;
            public VisualTreeAsset contentTree;

            public ToolbarsTab(string tabName, Button tabButton, Vector2 minWindowSize = default)
            {
                this.tabName = tabName;
                this.tabButton = tabButton;

                if (minWindowSize == Vector2.zero || minWindowSize.magnitude < 0)
                {
                    this.minWindowSize = new Vector2(300, 300);
                    return;
                }

                this.minWindowSize = minWindowSize;
            }
        }

        internal class ChannelData
        {
            public bool isMarkedForDeletion;
            public string name;
        }
        
        #endregion

        #region Fields

        private static EditorWindow m_Window;

        private readonly List<ToolbarsTab> toolbar_tabs = new()
        {
            new ToolbarsTab("Home", null),
            new ToolbarsTab("Channels", null, new Vector2(780, 200)),
            new ToolbarsTab("Categories", null)
        };

        private VisualElement m_Root;
        private VisualTreeAsset m_RootTree;
        private TextField _newChannelNameField;

        // Generator
        private readonly List<ChannelData> _channelEntries = new();
        private VisualTreeAsset _channelItemTemplate;
        private ScrollView _channelListContainer;
        private DropdownField _categoriesDropdownField;
        private TextField _searchField;
        private Button channelAddButton;
        private Button channelSaveButton;
        private VisualElement m_ContentRoot;
        private ToolbarsTab _selectedTab;
        private bool _needToRecompile;
        
        // Categories
        private Categories _categoriesAsset;
        private VisualTreeAsset _categoryItemTemplate;
        private ScrollView _categoryListContainer;
        private TextField _newCategoryField;
        private Button _addCategoryButton;
        private TextField _categorySearchField;
        
        #endregion

        private void OnEnable()
        {
            LoadChannelsFromReflection();
            if (m_Root != null) PopulateChannelList();
        }

        private void OnDisable()
        {
            _searchField?.UnregisterValueChangedCallback(OnSearchTextChanged);
            _categoriesDropdownField?.UnregisterValueChangedCallback(OnDropdownChanges);
        }

        [MenuItem("Tools/Eventus.System/Open Window")]
        public static void ShowWindow()
        {
            m_Window = GetWindow<EditorWindow>();
            m_Window.minSize = new Vector2(300, 300);
        }

        public void CreateGUI()
        {
            m_Root = rootVisualElement;

            // Main root
            m_RootTree = EditorUtils.LoadUxml("Window");
            m_RootTree.CloneTree(m_Root);

            // Get current content
            m_ContentRoot = m_Root.Q<VisualElement>("content");

            // Get SO Categories
            _categoriesAsset = EditorUtils.FindCategories();

            // Get windows and setting tabs
            foreach (var tab in toolbar_tabs)
            {
                tab.contentTree = EditorUtils.LoadUxml(tab.tabName);
                tab.contentTree.CloneTree(m_ContentRoot);
            }

            InitializeToolbar();
            InitializeComponents();
            InitializeCategoriesContent();
            PopulateCategoriesDropdown();
        }

        private void InitializeToolbar()
        {
            for (var i = 0; i < toolbar_tabs.Count; i++)
            {
                var tab = toolbar_tabs[i];
                var tabName = tab.tabName.ToLower();

                var rootButton = m_Root.Q<Button>(MakeToolbarId(tabName, "btn"));
                var rootContent = m_Root.Q<VisualElement>(MakeToolbarId(tabName, "content"));

                if (rootButton != null && rootContent != null)
                {
                    tab.tabButton = rootButton;
                    tab.tabContent = rootContent;
                    tab.index = i;
                    tab.tabButton.clicked += () => SelectToolbarTab(tab.index);
                }
                else
                {
                    EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.ToolbarTabNotfound, "OK");
                }

                continue;

                // Interpolation
                string MakeToolbarId(string nameToInterpolate, string suffix) => $"tbar-{nameToInterpolate.ToLower()}-{suffix}";
            }

            // Setting a home with first tab
            if (toolbar_tabs[0].tabButton != null && toolbar_tabs[0].tabContent != null)
                SelectToolbarTab(0);
            else
                Debug.LogError("[Eventus] Failed to initialize first toolbar tab.");
        }

        private void InitializeComponents()
        {
            _channelItemTemplate = EditorUtils.LoadUxml("ChannelItem");
            _channelListContainer = m_Root.Q<ScrollView>("channels-list-scrollview");
            _categoriesDropdownField = m_Root.Q<DropdownField>("channels-categories-field");
            _searchField = m_Root.Q<TextField>("channels-search-field");
            _newChannelNameField = m_Root.Q<TextField>("new-channel-name-field");
            
            _categoryItemTemplate = EditorUtils.LoadUxml("CategoryItem");
            _categoryListContainer = m_Root.Q<ScrollView>("categories-list-scrollview");
            _newCategoryField = m_Root.Q<TextField>("new-category-name-field");
            _addCategoryButton = m_Root.Q<Button>("add-category-button");
            _categorySearchField = m_Root.Q<TextField>("categories-search-field");
            
            m_ContentRoot.Q<Button>("save-changes-button").clicked += GenerateChannel;
            m_ContentRoot.Q<Button>("revert-changes-button").clicked += RevertChanges;
            m_ContentRoot.Q<Button>("home-doc-en-btn").clicked += () => Application.OpenURL(Global.DOC_EN_URL);
            m_ContentRoot.Q<Button>("home-doc-pt-btn").clicked += () => Application.OpenURL(Global.DOC_PT_URL);

            _searchField.RegisterValueChangedCallback(OnSearchTextChanged);
            _categorySearchField.RegisterValueChangedCallback(OnCategorySearchChanged);
            _categoriesDropdownField.RegisterValueChangedCallback(OnDropdownChanges);
            
            m_ContentRoot.Q<Button>("add-channel-button").clicked += () =>
            {
                AddNewChannel(_newChannelNameField.value);
                _newChannelNameField.value = "";
            };
            
            _addCategoryButton.clicked += () =>
            {
                AddNewCategory(_newCategoryField.value);
                _newCategoryField.value = "";
            };

            PopulateChannelList();
        }

        private void PopulateCategoriesDropdown()
        {
            if (_categoriesDropdownField == null) return;
            _categoriesDropdownField.choices.Clear();
            foreach (var category in _categoriesAsset.categories) _categoriesDropdownField.choices.Add(category);
            _categoriesDropdownField.index = 0;
        }
        
        private void InitializeCategoriesContent()
        {
            PopulateCategoryList();
        }
        
        private void SelectToolbarTab(int index)
        {
            PopulateCategoriesDropdown();
            var clampedIndex = Mathf.Clamp(index, 0, toolbar_tabs.Count - 1);

            for (var i = 0; i < toolbar_tabs.Count; i++)
            {
                var tab = toolbar_tabs[i];

                if (!i.Equals(clampedIndex))
                {
                    tab.tabButton.RemoveFromClassList("toolbar-select-btn");
                    tab.tabContent.style.display = DisplayStyle.None;
                }
                else
                {
                    tab.tabButton.AddToClassList("toolbar-select-btn");
                    tab.tabContent.style.display = DisplayStyle.Flex;

                    if (!m_Window) continue;
                    m_Window.minSize = tab.minWindowSize;
                    _selectedTab = tab;
                    
                    UpdateWindowTitle();
                }
            }
        }

        private void OnDropdownChanges(ChangeEvent<string> evt)
        {
            PopulateChannelList();
        }
        
        private void OnSearchTextChanged(ChangeEvent<string> evt)
        {
            PopulateChannelList();
        }

        private void UpdateWindowTitle()
        {
            if (m_Window == null || _selectedTab == null) return;
            var changesWarning = _needToRecompile ? EditorMessages.UnsavedTitle : "";
            m_Window.titleContent =
                new GUIContent($"{EditorMessages.WindowTitle} ({_selectedTab.tabName}){changesWarning}");
        }

        #region Channel Management Logic

        private void LoadChannelsFromReflection()
        {
            _channelEntries.Clear();
            
            var channelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Channel)));

            foreach (var type in channelTypes)
            {
                var entry = new ChannelData
                {
                    name = type.Name,
                    isMarkedForDeletion = false
                };
                _channelEntries.Add(entry);
            }
        }

        private void PopulateChannelList()
        {
            if (_channelListContainer == null || _channelItemTemplate == null) return;

            _channelListContainer.Clear();
            
            var searchQuery = _searchField.value?.ToLower().Trim() ?? "";
            
            var entriesToDraw = _channelEntries
                .Where(e => !e.isMarkedForDeletion && (string.IsNullOrEmpty(searchQuery) || e.name.ToLower().Contains(searchQuery)))
                .OrderBy(e => e.name);

            foreach (var entryData in entriesToDraw)
            {
                var newItem = _channelItemTemplate.Instantiate();
                var nameField = newItem.Q<TextField>("channel-name-label");
                var removeButton = newItem.Q<Button>("delete-button");

                nameField.value = entryData.name;
                
                nameField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == entryData.name) return;
                    _needToRecompile = true;
                    UpdateWindowTitle();
                    entryData.name = evt.newValue;
                });
                
                removeButton.clicked += () =>
                {
                    if (!EditorUtility.DisplayDialog(EditorMessages.ConfirmDeleteTitle,
                            string.Format(EditorMessages.ConfirmDeleteBody, entryData.name), "Yes", "No")) return;
        
                    entryData.isMarkedForDeletion = true;
                    _needToRecompile = true;
                    PopulateChannelList();
                    UpdateWindowTitle();
                };

                _channelListContainer.Add(newItem);
            }
        }

        private void AddNewChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.ErrorNameEmpty, "OK");
                return;
            }

            if (!Helper.IsValidEnumName(channelName))
            {
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.ErrorNameInvalidToEnum, "OK");
                return;
            }

            channelName = Regex.Replace(channelName, @"\s+", "");
            
            if (!char.IsLetter(channelName[0]))
            {
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.ErrorNameInvalidChar, "OK");
                return;
            }

            if (_channelEntries.Any(e =>
                    !e.isMarkedForDeletion && e.name.Equals(channelName, StringComparison.OrdinalIgnoreCase)))
            {
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.ErrorNameExists, "OK");
                return;
            }

            var currentCategory = GetCurrentCategory() == Global.DEFAULT_CATEGORY ? "" : GetCurrentCategory() + "_";

            _channelEntries.Add(new ChannelData
            {
                name = $"{currentCategory}{channelName}",
            });

            _needToRecompile = true;
            UpdateWindowTitle();
            PopulateChannelList();
        }

        private void RevertChanges()
        {
            if (!_needToRecompile)
            {
                EditorUtility.DisplayDialog("Revert Changes", "There are no unsaved changes to revert.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm Revert",
                    "Are you sure you want to discard all unsaved changes?",
                    "Yes, Discard Changes", "Cancel")) return;

            LoadChannelsFromReflection();
            PopulateChannelList();

            _needToRecompile = false;
            UpdateWindowTitle();
        }

        private void GenerateChannel()
        {
            if (!_needToRecompile)
            {
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.ErrorCodeRecompile, "OK");
                return;
            }

            var scriptPath = EditorUtils.FindChannelScriptPath();

            Debug.Log(scriptPath);
            if (string.IsNullOrEmpty(scriptPath))
            {
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.ErrorFindChannelScript, "OK");
                return;
            }

            try
            {
                var finalEntries = _channelEntries.Where(e => !e.isMarkedForDeletion).OrderBy(e => e.name).ToList();
                var newContentBuilder = new StringBuilder();

                // Template
                newContentBuilder.AppendLine("//////////////////////////////////////////////////////////////////");
                newContentBuilder.AppendLine("//                                                              //");
                newContentBuilder.AppendLine("//      AUTO-GENERATED BY THE EVENTUS CHANNEL MANAGER.          //");
                newContentBuilder.AppendLine("//                                                              //");
                newContentBuilder.AppendLine("//      DO NOT EDIT THIS FILE MANUALLY!                         //");
                newContentBuilder.AppendLine("//      Your changes will be overwritten. Use the Editor        //");
                newContentBuilder.AppendLine("//      Window at 'Tools > Eventus > Hub' to manage channels.   //");
                newContentBuilder.AppendLine("//                                                              //");
                newContentBuilder.AppendLine("//////////////////////////////////////////////////////////////////\n");
                newContentBuilder.AppendLine("namespace Eventus.Core");
                newContentBuilder.AppendLine("{\n");
                newContentBuilder.AppendLine("");
                foreach (var entry in finalEntries)
                {
                    newContentBuilder.AppendLine($"    public sealed class {entry.name} : Channel" + "{}\n");
                }
                newContentBuilder.AppendLine("}");
                
                // Reflection
                File.WriteAllText(scriptPath, newContentBuilder.ToString());
                Debug.Log("[Eventus] 'Channels.cs' has been updated. Scheduling UI update after recompilation...");
                EditorApplication.delayCall += OnAfterRecompile;
                AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Eventus] Failed to write to 'Channel.cs'. Error: {e.Message}. Contact the support!");
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.ErrorFileSaveFailed, "OK");
            }
        }

        private void OnAfterRecompile()
        {
            _needToRecompile = false;
            EditorApplication.delayCall -= OnAfterRecompile;

            Debug.Log("[Eventus] Recompilation finished. Updating window state.");
            EditorUtility.DisplayDialog(EditorMessages.SuccessTitle, EditorMessages.SuccessFileSave, "OK");

            UpdateWindowTitle();
            LoadChannelsFromReflection();
            PopulateChannelList();
        }

        #endregion
        
        #region Category Management Logic
        
        private string GetCurrentCategory()
        {
            if (_categoriesDropdownField == null || _categoriesDropdownField.index < 0 ||
                _categoriesDropdownField.choices == null ||
                _categoriesDropdownField.index >= _categoriesDropdownField.choices.Count)
                return Global.DEFAULT_CATEGORY;
            return _categoriesDropdownField.choices[_categoriesDropdownField.index];
        }
        
        private void OnCategorySearchChanged(ChangeEvent<string> evt)
        {
            PopulateCategoryList();
        }
        
        private void AddNewCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.CategoryEmptyName, "OK");
                return;
            }

            if (_categoriesAsset.categories.Contains(categoryName))
            {
                EditorUtility.DisplayDialog(EditorMessages.ErrorTitle, EditorMessages.CategoryNameExists, "OK");
                return;
            }

            _categoriesAsset.categories.Add(categoryName);
            EditorUtility.SetDirty(_categoriesAsset);
            PopulateCategoryList();
        }
        
        private void PopulateCategoryList()
        {
            if (_categoryListContainer == null || _categoryItemTemplate == null || _categoriesAsset == null) return;

            _categoryListContainer.Clear();

            var search = _categorySearchField?.value?.ToLowerInvariant().Trim();
            var filteredCategories = string.IsNullOrEmpty(search)
                ? _categoriesAsset.categories
                : _categoriesAsset.categories.Where(c => c.ToLowerInvariant().Contains(search)).ToList();

            foreach (var category in filteredCategories)
            {
                var newItem = _categoryItemTemplate.Instantiate();
                var nameField = newItem.Q<TextField>("category-name-field");
                var removeButton = newItem.Q<Button>("delete-button");

                var isDefault = category == Global.DEFAULT_CATEGORY;
                nameField.value = isDefault ? category + " [ReadOnly]" : category;
                var indexInSo = _categoriesAsset.categories.IndexOf(category);

                if (isDefault)
                {
                    nameField.isReadOnly = true;

                    var inputElement = nameField.Q<VisualElement>("unity-text-input");
                    if (inputElement != null)
                    {
                        inputElement.style.borderBottomWidth = 0;
                        inputElement.style.borderTopWidth = 0;
                        inputElement.style.borderLeftWidth = 0;
                        inputElement.style.borderRightWidth = 0;

                        inputElement.style.backgroundColor = new Color(0, 0, 0, 0);
                        inputElement.style.color = new Color(0.5f, 0.5f, 0.5f);
                    }

                    nameField.style.color = new Color(0.5f, 0.5f, 0.5f);
                    nameField.style.unityFontStyleAndWeight = FontStyle.Italic;
                    removeButton.style.display = DisplayStyle.None;
                }
                else
                {
                    nameField.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue == _categoriesAsset.categories[indexInSo]) return;
                        _categoriesAsset.categories[indexInSo] = evt.newValue;
                        EditorUtility.SetDirty(_categoriesAsset);
                    });
                }

                removeButton.clicked += () =>
                {
                    if (!EditorUtility.DisplayDialog(EditorMessages.RemoveCategoryTitle,
                            string.Format(EditorMessages.ConfirmCategoryDeleteBody, category), "Yes", "No"))
                        return;

                    _categoriesAsset.categories.RemoveAt(indexInSo);
                    EditorUtility.SetDirty(_categoriesAsset);
                    
                    PopulateCategoryList();
                    PopulateCategoriesDropdown();
                };

                _categoryListContainer.Add(newItem);
            }
        }
        
        #endregion
    }
}