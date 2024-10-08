﻿/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using HttpProgress;
using System.Text.Json;
using DS4Windows;
using MarkdownEngine = MdXaml.Markdown;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class ChangelogViewModel
    {
        private string markdown;

        public string Markdown
        {
            get => markdown;
            set
            {
                markdown = value;
                MarkdownChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler MarkdownChanged;

        public async Task DisplayChangelog()
        {
            var changelog = await Changelog.GetChangelogMarkdown(true);
            Markdown = changelog;
        }
    }
}
