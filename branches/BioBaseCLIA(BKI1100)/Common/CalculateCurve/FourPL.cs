﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioBaseCLIA.CalculateCurve;

namespace BioBaseCLIA.CalculateCurve
{
    class FourPL : Calculater
    {
        public FourPL()
        {
        }
        public FourPL(List<double> pars)
        {
            _pars = new double[4];
            _pars[0] = pars[0];
            _pars[1] = pars[1];
            _pars[2] = pars[2];
            _pars[3] = pars[3];
        }
        public override void AddData(List<Data_Value> data)
        {
            _pars = new double[4];
            _fitData.Clear();
            foreach (Data_Value dv in data)
                _fitData.Add(new Data_Value() { Data = dv.Data, DataValue = dv.DataValue });
        }
        public override void Fit()
        {
            List<double> s1 = new List<double>();
            List<double> s2 = new List<double>();
            List<double> s3 = new List<double>();
            foreach (Data_Value dv in _fitData)
            {
                s1.Add(dv.Data);
                s2.Add(dv.DataValue);
                s3.Add(1);
            }

            CMLxLM lm = new CMLxLM(4, s1, s2, s3, false);
            lm.Fit();
            _pars[0] = lm.m_a[0];
            _pars[1] = lm.m_a[1];
            _pars[2] = lm.m_a[2];
            _pars[3] = lm.m_a[3];
        }
        public override string StrFunc
        {
            get { return "(" + _pars[0] + "-" + _pars[3] + ")/(1+(X/" + _pars[2] + ")^" + _pars[1] + ")+" + _pars[3]; }
        }
        public override double GetResult(double xValue)
           
        {
            if (xValue < 0)
                xValue = 0;
            if (_pars == null)
            { return 0; }
            return (_pars[0] - _pars[3]) / (1 + Math.Pow(xValue / _pars[2], _pars[1])) + _pars[3];
        }
        public override double GetResultInverse(double yValue)//计算浓度
        {
            return _pars[2] * (Math.Pow((((_pars[0] - _pars[3]) / (yValue - _pars[3])) - 1), (1 / _pars[1])));
        }
        public override string StrPars
        {
            get { return _pars[0] + "|" + _pars[1] + "|" + _pars[2] + "|" + _pars[3]; }
        }
        public override int LeastNum
        {
            get { return 4; }
        }
    }
}
