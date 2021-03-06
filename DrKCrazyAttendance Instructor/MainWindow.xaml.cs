﻿using DrKCrazyAttendance;
using DrKCrazyAttendance.Properties;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DrKCrazyAttendance_Instructor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SettingsForm settingsForm;
        private About about;
        private List<CourseEditor> editors = new List<CourseEditor>();
        private AttendanceReport attendanceReport;
        private List<Course> courses = new List<Course>();

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            Title += " Logged in as: "+ Settings.Default.Instructor;
            OriginalTitle = Title;
        }

        #region Properties
        //For use as a singleton
        public static MainWindow Instance
        {
            get;
            private set;
        }

        public string OriginalTitle
        {
            get;
            private set;
        }
        #endregion

        public void LoadCourses()
        {
            lstCourses.Items.Clear();
            classroomCombo.Items.Clear();
            daysCombo.Items.Clear();

            classroomCombo.Items.Add("Classrooms");
            classroomCombo.SelectedIndex = 0;
            daysCombo.Items.Add("Days");
            daysCombo.SelectedIndex = 0;

            if (DatabaseManager.TestConnection())
            {
                Title = OriginalTitle + " - Connected";

                List<string> classrooms = Course.GetClassrooms();
                foreach (string classroom in classrooms)
                {
                    classroomCombo.Items.Add(classroom);
                }

                
                string query = @"SELECT DISTINCT days FROM Courses ORDER BY days";
                MySqlDataReader rdr = null;
                using (rdr = DatabaseManager.GetDataReaderFromQuery(query))
                {
                    if (rdr != null)
                    {
                        try
                        {
                            while (rdr.Read())
                            {
                                daysCombo.Items.Add(rdr.GetString(0));
                            }
                        }
                        catch (MySqlException ex)
                        {
                            Console.WriteLine("Mysql Error {0}", ex);
                        }
                    }
                }

                courses = Course.GetCoursesByInstructor(Settings.Default.Instructor);
                SetCourseList(courses);

                btnAdd.IsEnabled = true;
            }
            else
            {
                Title = OriginalTitle + " - Disconnected";
                btnAdd.IsEnabled = false;

                MessageBox.Show("Error! Could not connect to database. Please confirm settings are correct. If problem persists, please contact IT.",
                        "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateCourseEditor(Course course)
        {
            CourseEditor ce = null;
            if (course == null)
            {
                ce = new CourseEditor();
            }
            else
            {
                bool cExists = false;
                //make sure we aren't already editing a course
                foreach (CourseEditor c in editors) {
                    if (course.Id != 0 && course.Id == c.CourseViewModel.Id)
                    {
                        c.Focus();
                        c.WindowState = WindowState.Normal;
                        cExists = true;
                        break;
                    }
                }
                if (!cExists)
                {
                    ce = new CourseEditor(course);
                }
            }

            if (ce != null)
            {
                ce.Owner = this;
                ce.Show();
                ce.Closed += OnCourseEditorClose;
                editors.Add(ce);
            }
        }

        private void Filter(string classroom, string days)
        {
            IQueryable<Course> cs = courses.AsQueryable();

            if (days != null)
                cs = cs.Where(course => course.FriendlyDays.Equals(days));
            if (classroom != null)
                cs = cs.Where(course => course.Classroom.Equals(classroom));

            SetCourseList(cs.ToList<Course>());
        }

        private void SetCourseList(List<Course> courses)
        {
            lstCourses.Items.Clear();
            foreach (Course c in courses)
            {
                lstCourses.Items.Add(c);
            }
        }

        #region events
        private void menuSettings_Click(object sender, RoutedEventArgs e)
        {
            settingsForm = new SettingsForm();
            settingsForm.Owner = this;
            settingsForm.ShowDialog();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var confirmDelete = MessageBox.Show("Are you sure you want to delete this Course?",
                                    "Confirm", MessageBoxButton.OKCancel);

            if (confirmDelete == MessageBoxResult.OK && lstCourses.SelectedItem != null)
            {
                Course course = (Course)lstCourses.SelectedItem;
                Course.Remove(course);
                lstCourses.Items.Remove(course);
            }

        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            CreateCourseEditor(null);
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (lstCourses.SelectedItem != null)
            {
                CreateCourseEditor((Course)lstCourses.SelectedItem);
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstCourses.SelectedItem != null)
            {
                CreateCourseEditor((Course)lstCourses.SelectedItem);
            }
        }

        private void btnClone_Click(object sender, RoutedEventArgs e)
        {
            if (lstCourses.SelectedItem != null)
            {
                Course course = (Course)lstCourses.SelectedItem;
                Course cloneCourse = new Course(course);
                CreateCourseEditor(cloneCourse);
            }
        }

        private void menuClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LoadCourses();
        }

        private void OnCourseEditorClose(object sender, EventArgs e)
        {
            CourseEditor ce = (CourseEditor)sender;
            editors.Remove(ce);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure you want to close the program?",
                                            "Confirm", MessageBoxButton.YesNo);
            if (confirmResult == MessageBoxResult.Yes)
            {
                if (settingsForm != null && settingsForm.IsLoaded)
                {
                    confirmResult = MessageBox.Show("Are you sure you close without saving the settings?",
                                            "Confirm", MessageBoxButton.YesNo);
                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        settingsForm.Close();
                    }
                    else
                    {
                        //cancel if the user doesn't confirm
                        e.Cancel = true;
                    }
                }
                if (about != null && about.IsLoaded)
                {
                    about.Close();
                }

                if (attendanceReport != null && attendanceReport.IsVisible)
                {
                    attendanceReport.Close();
                }

                //close every opened course editor
                for (int i = editors.Count-1; i >= 0; i--)
                {
                    if (editors[i] != null && editors[i].IsLoaded)
                    {
                        editors[i].Close();
                    }
                }

            }
            else
            {
                //cancel if the user doesn't confirm
                e.Cancel = true;
            }
        }

        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            if (about != null && about.IsLoaded)
            {
                about.Focus();
                about.WindowState = WindowState.Normal;
            }
            else
            {
                about = new About();
                about.Owner = this;
                about.Show();
            }
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            if (lstCourses.SelectedItem != null)
            {
                Course course = (Course)lstCourses.SelectedItem;
                attendanceReport = new AttendanceReport(course);
                attendanceReport.Owner = this;
                attendanceReport.Show();
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string classroom = classroomCombo.SelectedItem as string;
            if (classroomCombo.SelectedIndex == 0)
                classroom = null;

            string days = daysCombo.SelectedItem as string;
            if (daysCombo.SelectedIndex == 0)
                days = null;

            Filter(classroom, days);
        }

        private void lstCourses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool enable = lstCourses.SelectedItem != null;
            btnDelete.IsEnabled = enable;
            btnClone.IsEnabled = enable;
            btnEdit.IsEnabled = enable;
            btnReport.IsEnabled = enable;
        }
        #endregion

    }
}
