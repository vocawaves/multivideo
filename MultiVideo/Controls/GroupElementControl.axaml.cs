using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using MultiVideo.Models;
using MultiVideo.ViewModels;
using MultiVideo.Views;

namespace MultiVideo.Controls;

[PseudoClasses(":playing", ":pressed")]
public class GroupElementControl : TemplatedControl
{
    public static readonly StyledProperty<GroupWrapper?> GroupProperty =
        AvaloniaProperty.Register<GroupElementControl, GroupWrapper?>(
            nameof(Group));

    public GroupWrapper? Group
    {
        get => GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    public static readonly StyledProperty<IList<GroupWrapper>?> ParentCollectionProperty =
        AvaloniaProperty.Register<GroupElementControl, IList<GroupWrapper>?>(
            nameof(ParentCollection));

    public IList<GroupWrapper>? ParentCollection
    {
        get => GetValue(ParentCollectionProperty);
        set => SetValue(ParentCollectionProperty, value);
    }

    public static readonly StyledProperty<ICommand> MainCommandProperty =
        AvaloniaProperty.Register<GroupElementControl, ICommand>(
            nameof(MainCommand));

    public ICommand MainCommand
    {
        get => GetValue(MainCommandProperty);
        set => SetValue(MainCommandProperty, value);
    }

    public static readonly StyledProperty<object?> MainCommandParameterProperty =
        AvaloniaProperty.Register<GroupElementControl, object?>(
            nameof(MainCommandParameter));

    public object? MainCommandParameter
    {
        get => GetValue(MainCommandParameterProperty);
        set => SetValue(MainCommandParameterProperty, value);
    }

    static GroupElementControl()
    {
        PressedMixin.Attach<GroupElementControl>();
        GroupProperty.Changed.AddClassHandler<GroupElementControl>((x, e) =>
        {
            if (e.OldValue is GroupWrapper oldGroup)
                oldGroup.PropertyChanged -= x.OnGroupPropertyChanged;
            if (e.NewValue is GroupWrapper newGroup)
                newGroup.PropertyChanged += x.OnGroupPropertyChanged;
        });
    }

    private void OnGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Group.IsPlaying)) 
            PseudoClasses.Set(":playing", Group?.IsPlaying ?? false);
    }

    public async Task EditVideoGroup()
    {
        if (Group is null)
            return;

        var editWindow = new VideoGroupEditWindow
        {
            DataContext = new VideoGroupEditViewModel
            {
                Group = Group
            }
        };

        var tl = TopLevel.GetTopLevel(this);
        if (tl is not Window parent)
            return;
        var result = await editWindow.ShowDialog<GroupWrapper?>(parent);
        if (result is null)
            return;
        Group = result;
    }

    public void RemoveVideoGroup()
    {
        if (Group is null || ParentCollection is null)
            return;
        //find the index of the group
        var index = ParentCollection?.IndexOf(ParentCollection.First(x => x == this.Group));
        if (index is null)
            return;
        //remove the group
        ParentCollection?.RemoveAt(index.Value);
    }
}