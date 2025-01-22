namespace ADHelpers.wpf
{
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices.AccountManagement;
    using System.IO;
    using System.Security.Principal;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using System.Xml.Linq;
    using ADHelpers.DataAccess;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GetUsernamesListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            InputContainer.Children.Clear();
            TextBox input = new TextBox();
            InputContainer.Children.Add(input);
            Button processButton = new Button
            {
                Content = "Process",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 205, 0, 0),
            };

            processButton.Click += (s, e) =>
            {
                TextBox output = new TextBox();
                OutputContainer.Children.Add(output);
                UserNamesRecord unames = ((UserPrincipal)Principal.FindByIdentity(new PrincipalContext(ContextType.Domain), IdentityType.SamAccountName, input.Text)).GetUserNames();
                output.Text += "SamAccountName: " + unames.SamAccountName + Environment.NewLine;
                output.Text += "UserPrincipalName: " + unames.UserPrincipalName + Environment.NewLine;
                output.Text += "TargetAddress: " + unames.TargetAddress + Environment.NewLine;
                output.Text += "Name: " + unames.Name + Environment.NewLine;
                output.Text += "DisplayName: " + unames.DisplayName + Environment.NewLine;
                output.Text += "EmailAddress: " + unames.EmailAddress + Environment.NewLine;
                output.Text += "MailNickname: " + unames.MailNickname + Environment.NewLine;
                output.Text += "ProxyAddresses: " + unames.ProxyAddresses;
            };

            MainGrid.Children.Add(processButton);
        }

        private void CompareADGroupMembershipsListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            InputContainer.Children.Clear();
            TextBox input = new TextBox();
            InputContainer.Children.Add(input);
            Button processButton = new Button
            {
                Content = "Process",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 205, 0, 0)
            };

            processButton.Click += (s, e) =>
            {
                TextBox output = new TextBox();
                OutputContainer.Children.Add(output);
                Tuple<Dictionary<Principal, HashSet<Principal>>, HashSet<Principal>> data = PrincipalExtentions.CompareGroups(PrincipalExtentions.GetPrincipals(input.Text.Split(Environment.NewLine), IdentityType.SamAccountName));
                output.Text += "Groups common to all: ";
                foreach (Principal group in data.Item2)
                {
                    output.Text += group.Name + Environment.NewLine;
                }

                foreach (KeyValuePair<Principal, HashSet<Principal>> uniqueGroups in data.Item1)
                {
                    output.Text += Environment.NewLine;
                    output.Text += $"Groups only {uniqueGroups.Key.Name} has: {Environment.NewLine}";
                    foreach (Principal group in uniqueGroups.Value)
                    {
                        output.Text += group.Name + Environment.NewLine;
                    }

                }
            };

            MainGrid.Children.Add(processButton);
        }

        private void GetLockoutStatusListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            InputContainer.Children.Clear();
            TextBox input = new TextBox();
            InputContainer.Children.Add(input);
            Button processButton = new Button
            {
                Content = "Process",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 205, 0, 0)
            };

            processButton.Click += (s, e) =>
            {
                TextBox output = new TextBox();
                OutputContainer.Children.Add(output);
                LockoutStatusRecord lockoutStatusRecord = ((AuthenticablePrincipal)Principal.FindByIdentity(new PrincipalContext(ContextType.Domain), IdentityType.SamAccountName, input.Text)).GetLockoutStatus();
                output.Text += "DistinguishedName: " + lockoutStatusRecord.DistinguishedName + Environment.NewLine;
                output.Text += "SamAccountName: " + lockoutStatusRecord.SamAccountName + Environment.NewLine;
                output.Text += "BadLogonCount: " + lockoutStatusRecord.BadLogonCount + Environment.NewLine;
                output.Text += "LastBadPasswordAttempt: " + lockoutStatusRecord.LastBadPasswordAttempt + Environment.NewLine;
                output.Text += "LastLogon: " + lockoutStatusRecord.LastLogon + Environment.NewLine;
                output.Text += "Enabled: " + lockoutStatusRecord.Enabled + Environment.NewLine;
                output.Text += "AccountExpirationDate: " + lockoutStatusRecord.AccountExpirationDate + Environment.NewLine;
                output.Text += "AccountLockoutTime: " + lockoutStatusRecord.AccountLockoutTime;
                output.Text += "PwdExpires: " + lockoutStatusRecord.PwdExpires + Environment.NewLine;
                output.Text += "LastPasswordSet: " + lockoutStatusRecord.LastPasswordSet;
            };

            MainGrid.Children.Add(processButton);
        }

        private void ProcessLeaversListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            InputContainer.Children.Clear();
            TextBox input = new TextBox();
            InputContainer.Children.Add(input);
            Button processButton = new Button
            {
                Content = "Process",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 205, 0, 0)
            };

            processButton.Click += (s, e) =>
            {
                PrincipalExtentions.ProcessLeavers(input.Text.Split(Environment.NewLine), File.ReadAllLines("LicencedGroups.csv"));
                TextBox output = new TextBox();
                output.Text = "done.";
                OutputContainer.Children.Add(output);
            };
            
            MainGrid.Children.Add(processButton);
        }
    }
}