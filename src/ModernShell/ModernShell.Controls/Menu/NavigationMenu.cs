using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ModernShell.Controls.Menu
{
    public class NavigationMenu : Control
    {
        static NavigationMenu()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationMenu), new FrameworkPropertyMetadata(typeof(NavigationMenu)));
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(IEnumerable<INavigationMenuDescriptor>),
            typeof(NavigationMenu),
            new PropertyMetadata(null, ItemsSourcePropertyChanged));

        public static readonly DependencyProperty MenuItemsProperty = DependencyProperty.Register(
            "MenuItems",
            typeof(ICollection<NavigationMenuItem>),
            typeof(NavigationMenu),
            new PropertyMetadata(null, MenuItemsPropertyChanged));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem",
            typeof(INavigationMenuDescriptor),
            typeof(NavigationMenu),
            new FrameworkPropertyMetadata(null, OnSelectedItemChanged)
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

        public static readonly DependencyProperty SelectedMenuItemProperty = DependencyProperty.Register(
            "SelectedMenuItem",
            typeof(NavigationMenuItem),
            typeof(NavigationMenu),
            new PropertyMetadata(null, SelectedMenuItemPropertyChanged));

        public static readonly DependencyProperty RootItemTemplateProperty = DependencyProperty.Register(
            "RootItemTemplate",
            typeof(DataTemplate),
            typeof(NavigationMenu),
            new PropertyMetadata(null));

        public static readonly DependencyProperty LeafItemTemplateProperty = DependencyProperty.Register(
            "LeafItemTemplate",
            typeof(DataTemplate),
            typeof(NavigationMenu),
            new PropertyMetadata(null));

        public DataTemplate RootItemTemplate
        {
            set { SetValue(RootItemTemplateProperty, value); }
            get { return (DataTemplate)GetValue(RootItemTemplateProperty); }
        }

        public DataTemplate LeafItemTemplate
        {
            get { return (DataTemplate)GetValue(LeafItemTemplateProperty); }
            set { SetValue(LeafItemTemplateProperty, value); }
        }

        public ICollection<NavigationMenuItem> MenuItems
        {
            set { SetValue(MenuItemsProperty, value); }
            get { return (ICollection<NavigationMenuItem>)GetValue(MenuItemsProperty); }
        }

        public NavigationMenuItem SelectedMenuItem
        {
            set { SetValue(SelectedMenuItemProperty, value); }
            get { return (NavigationMenuItem)GetValue(SelectedMenuItemProperty); }
        }

        public IEnumerable<INavigationMenuDescriptor> ItemsSource
        {
            set { SetValue(ItemsSourceProperty, value); }
            get { return (IEnumerable<INavigationMenuDescriptor>)GetValue(ItemsSourceProperty); }
        }

        public INavigationMenuDescriptor SelectedItem
        {
            set { SetValue(SelectedItemProperty, value); }
            get { return (INavigationMenuDescriptor)GetValue(SelectedItemProperty); }
        }

        private void UnselectOldSelectedMenuItems(NavigationMenuItem selectedMenuItem)
        {
            var oldSelectedMenuItems = MenuItems.Where(item => item.MenuItems != null)
                .SelectMany(item => item.MenuItems)
                .Concat(MenuItems)
                .Where(item => !Equals(item, selectedMenuItem))
                .Where(item => item.IsSelected)
                .ToArray();

            foreach (var item in oldSelectedMenuItems)
            {
                if (!(item.MenuItems?.Any(subItem => subItem.Equals(selectedMenuItem)) ?? false))
                {
                    item.IsSelected = false;
                }
            }
        }

        private static void SelectedMenuItemPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            //var menu = (NavigationMenu)sender;
            //var selectedNavigationItem = args.NewValue as NavigationMenuItem;
            //menu.MarkAsSelected(selectedNavigationItem);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var menu = (NavigationMenu)d;
            var selectedDescriptor = e.NewValue as INavigationMenuDescriptor;
            menu.MarkAsSelected(selectedDescriptor);
        }

        private static void MenuItemsPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
        }

        private static void ItemsSourcePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var control = (NavigationMenu)sender;
            control.SetupMenuItems(args.NewValue as IEnumerable<INavigationMenuDescriptor>);
        }

        private void MarkAsSelected(INavigationMenuDescriptor menuItemDescriptor)
        {
            if (menuItemDescriptor == null)
            {
                return;
            }

            var navigationItem = MenuItems.Where(x => x.MenuItems != null)
                                          .SelectMany(x => x.MenuItems)
                                          .Concat(MenuItems)
                                          .SingleOrDefault(x => x.DataContext == menuItemDescriptor);

            if (navigationItem != null)
            {
                navigationItem.IsSelected = true;
                UnselectOldSelectedMenuItems(navigationItem);
            }
        }

        private void SetupMenuItems(IEnumerable<INavigationMenuDescriptor> navigationMenuDescriptors)
        {
            var menuDescriptors = navigationMenuDescriptors?.ToArray();
            var menuItems = new List<NavigationMenuItem>();
            CalculateMenuItems(menuDescriptors, menuItems);
            MenuItems = menuItems;
            MenuItems?.FirstOrDefault()?.MarkAsSelected();
        }

        private void OnNavigationMenuItemSelectionChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            e.Handled = true;
            var selectedNavigationItem = sender as NavigationMenuItem;

            if (selectedNavigationItem != null && e.NewValue)
            {
                SelectMenuItem(selectedNavigationItem);
            }
        }

        private void SelectMenuItem(NavigationMenuItem item)
        {
            SelectedItem = item.DataContext as INavigationMenuDescriptor;
            SelectedMenuItem = item;
            UnselectOldSelectedMenuItems(item);
        }

        private void CalculateMenuItems(ICollection<INavigationMenuDescriptor> menuDescriptors, ICollection<NavigationMenuItem> menuItems)
        {
            if (menuDescriptors == null)
            {
                return;
            }

            foreach (var menuDescriptor in menuDescriptors)
            {
                var menuItem = new NavigationMenuItem { DataContext = menuDescriptor };
                menuItem.SetBinding(NavigationMenuItem.ItemTemplateProperty, new Binding(menuDescriptor.IsRoot ? "RootItemTemplate" : "LeafItemTemplate") {Source = this});
                menuItem.AddHandler(NavigationMenuItem.SelectionChangedEvent, new RoutedPropertyChangedEventHandler<bool>(OnNavigationMenuItemSelectionChanged));
                menuItems.Add(menuItem);

                if (!(menuDescriptor.Items?.Any() ?? false))
                {
                    continue;
                }

                if (menuItem.MenuItems == null)
                {
                    menuItem.MenuItems = new List<NavigationMenuItem>();
                }

                CalculateMenuItems(menuDescriptor.Items, menuItem.MenuItems);
            }
        }
    }
}
