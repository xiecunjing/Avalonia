// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Linq;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class PopupTests
    {
        [Fact]
        public void Setting_Child_Should_Set_Child_Controls_LogicalParent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Equal(child.Parent, target);
            Assert.Equal(((ILogical)child).LogicalParent, target);
        }

        [Fact]
        public void Clearing_Child_Should_Clear_Child_Controls_Parent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;
            target.Child = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Child_Control_Should_Appear_In_LogicalChildren()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Child_Should_Remove_From_LogicalChildren()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;
            target.Child = null;

            Assert.Equal(new ILogical[0], ((ILogical)target).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child = new Control();
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Child = child;

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child = new Control();
            var called = false;

            target.Child = child;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Child = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            target.Child = child1;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Child = child2;

            Assert.True(called);
        }

        [Fact]
        public void Setting_Child_Should_Not_Set_Childs_VisualParent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Null(((IVisual)child).VisualParent);
        }

        [Fact]
        public void PopupRoot_Should_Initially_Be_Null()
        {
            using (CreateServices())
            {
                var target = new Popup();

                Assert.Null(target.PopupRoot);
            }
        }

        [Fact]
        public void PopupRoot_Should_Have_Null_VisualParent()
        {
            using (CreateServices())
            {
                var target = new Popup();

                target.Open();

                Assert.Null(target.PopupRoot.GetVisualParent());
            }
        }

        [Fact]
        public void PopupRoot_Should_Have_Popup_As_LogicalParent()
        {
            using (CreateServices())
            {
                var target = new Popup();

                target.Open();

                Assert.Equal(target, target.PopupRoot.Parent);
                Assert.Equal(target, target.PopupRoot.GetLogicalParent());
            }
        }

        [Fact]
        public void PopupRoot_Should_Be_Detached_From_Logical_Tree_When_Popup_Is_Detached()
        {
            using (CreateServices())
            {
                var target = new Popup();
                var root = new TestRoot { Child = target };

                target.Open();

                var popupRoot = (ILogical)target.PopupRoot;

                Assert.True(popupRoot.IsAttachedToLogicalTree);
                root.Child = null;
                Assert.False(((ILogical)target).IsAttachedToLogicalTree);
            }
        }

        [Fact]
        public void PopupRoot_Should_Have_Template_Applied()
        {
            using (CreateServices())
            {
                var target = new Popup();
                var child = new Control();

                target.Open();

                Assert.Equal(1, target.PopupRoot.GetVisualChildren().Count());

                var templatedChild = target.PopupRoot.GetVisualChildren().Single();
                Assert.IsType<ContentPresenter>(templatedChild);
                Assert.Equal(target.PopupRoot, ((IControl)templatedChild).TemplatedParent);
            }
        }

        [Fact]
        public void Templated_Control_With_Popup_In_Template_Should_Set_TemplatedParent()
        {
            using (CreateServices())
            {
                PopupContentControl target;
                var root = new TestRoot
                {
                    Child = target = new PopupContentControl
                    {
                        Content = new Border(),
                        Template = new FuncControlTemplate<PopupContentControl>(PopupContentControlTemplate),
                    }
                };

                target.ApplyTemplate();
                var popup = (Popup)target.GetTemplateChildren().First(x => x.Name == "popup");
                popup.Open();
                var popupRoot = popup.PopupRoot;

                var children = popupRoot.GetVisualDescendants().ToList();
                var types = children.Select(x => x.GetType().Name).ToList();

                Assert.Equal(
                    new[]
                    {
                        "ContentPresenter",
                        "ContentPresenter",
                        "Border",
                    },
                    types);

                var templatedParents = children
                    .OfType<IControl>()
                    .Select(x => x.TemplatedParent).ToList();

                Assert.Equal(
                    new object[]
                    {
                        popupRoot,
                        target,
                        null,
                    },
                    templatedParents);
            }
        }

        private static IDisposable CreateServices()
        {
            var result = AvaloniaLocator.EnterScope();

            var styles = new Styles
            {
                new Style(x => x.OfType<PopupRoot>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<PopupRoot>(PopupRootTemplate)),
                    }
                },
            };

            var globalStyles = new Mock<IGlobalStyles>();
            globalStyles.Setup(x => x.Styles).Returns(styles);

            var renderInterface = new Mock<IPlatformRenderInterface>();

            AvaloniaLocator.CurrentMutable
                .Bind<ILayoutManager>().ToTransient<LayoutManager>()
                .Bind<IGlobalStyles>().ToFunc(() => globalStyles.Object)
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformMock())
                .Bind<IStyler>().ToTransient<Styler>()
                .Bind<IPlatformRenderInterface>().ToFunc(() => renderInterface.Object);
            
            return result;
        }

        private static IControl PopupRootTemplate(PopupRoot control)
        {
            return new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
            };
        }

        private static IControl PopupContentControlTemplate(PopupContentControl control)
        {
            return new Popup
            {
                Name = "popup",
                Child = new ContentPresenter
                {
                    [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                }
            };
        }

        private class PopupContentControl : ContentControl
        {
        }
    }
}
